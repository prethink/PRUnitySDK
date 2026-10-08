using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно модулей: какие части SDK входят в сборку этого проекта.
/// </summary>
/// <remarks>
/// Галки копятся до кнопки «Применить»: каждая смена define-символов пересобирает
/// весь проект, поэтому переключать модули по одному накладно.
/// Модули группы <see cref="PRModuleGroup.Integrations"/> идут отдельной вкладкой;
/// те из них, у кого общий слот, выбираются переключателем — включён один или ни одного.
/// </remarks>
public sealed class PRModulesWindow : ExtendedEditorWindow
{
    private const string MenuPath = "PRUnitySDK/Модули";

    private const string CoreFolder = "Assets/PRUnitySDK/Core";

    private const int IntegrationsTab = 1;

    private static readonly string[] TabTitles = { "Модули", "Интеграции" };

    private List<PRModuleInfo> modules = new();

    private HashSet<string> appliedDisabled = new();

    private HashSet<string> pendingDisabled = new();

    private HashSet<string> duplicateIds = new();

    private List<string> mismatchedTargets = new();

    private List<string> coreParts = new();

    private Dictionary<PRModuleInfo, List<string>> pluginModules = new();

    private HashSet<PRModuleInfo> missingPlugins = new();

    private readonly HashSet<string> collapsedSections = new();

    private string search = string.Empty;

    // Сериализуется, чтобы после «Применить» и пересборки окно осталось на той же вкладке.
    [SerializeField] private int tab;

    private Vector2 scroll;

    private GUIStyle wrappedMiniLabel;

    private GUIStyle radioLabel;

    [MenuItem(MenuPath, false, 1)]
    private static void Open()
    {
        var window = GetWindow<PRModulesWindow>();
        window.titleContent = new GUIContent("Модули");
        window.minSize = new Vector2(520f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        Reload(false);
    }

    private void OnProjectChange()
    {
        Reload(true);
        Repaint();
    }

    private void Reload(bool keepPending)
    {
        modules = PRModuleCatalog.Load();
        appliedDisabled = PRModuleDefines.ReadDisabledIds();
        mismatchedTargets = PRModuleDefines.FindMismatchedTargets();

        if (!keepPending)
            pendingDisabled = new HashSet<string>(appliedDisabled);

        duplicateIds = new HashSet<string>(modules
            .GroupBy(module => module.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key));

        coreParts = Directory.Exists(CoreFolder)
            ? Directory.GetDirectories(CoreFolder)
                .Select(Path.GetFileName)
                .Where(name => name != "Editor")
                .OrderBy(name => name.TrimStart('#', '@', '!'), StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<string>();

        pluginModules = modules
            .Where(module => File.Exists(module.Manifest.PluginModulesFile))
            .ToDictionary(module => module, module => ReadPluginModules(module.Manifest.PluginModulesFile));

        missingPlugins = new HashSet<PRModuleInfo>(modules.Where(module =>
            module.Manifest.PluginFolder.Length > 0 && !AssetDatabase.IsValidFolder(module.Manifest.PluginFolder)));
    }

    private static List<string> ReadPluginModules(string path)
    {
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private void OnGUI()
    {
        wrappedMiniLabel ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        radioLabel ??= new GUIStyle(EditorStyles.radioButton) { fontStyle = FontStyle.Bold };

        CreateHorizontalToolBar(DrawToolbar);
        tab = GUILayout.Toolbar(tab, TabTitles);
        DrawProjectWarnings();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (tab == IntegrationsTab)
            DrawIntegrations();
        else
            DrawLayers();

        EditorGUILayout.EndScrollView();
    }

    private void DrawLayers()
    {
        DrawLayer(PRModuleLayer.Public, "Публичная часть", DrawCore);
        DrawLayer(PRModuleLayer.Private, "Приватная часть", null);

        if (modules.Any(module => module.Layer == PRModuleLayer.Project && module.Manifest.Group != PRModuleGroup.Integrations))
            DrawLayer(PRModuleLayer.Project, "Проект", null);
    }

    private void DrawToolbar()
    {
        if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            Reload(true);

        int changes = pendingDisabled.Count(id => !appliedDisabled.Contains(id))
            + appliedDisabled.Count(id => !pendingDisabled.Contains(id));

        using (new EditorGUI.DisabledScope(changes == 0))
        {
            if (GUILayout.Button("Сбросить", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                pendingDisabled = new HashSet<string>(appliedDisabled);
        }

        using (new EditorGUI.DisabledScope(changes == 0 && mismatchedTargets.Count == 0))
        {
            string title = changes > 0 ? $"Применить ({changes})" : "Применить";
            if (GUILayout.Button(title, EditorStyles.toolbarButton, GUILayout.Width(100f)))
                Apply();
        }

        GUILayout.FlexibleSpace();
        search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200f));
    }

    private void DrawProjectWarnings()
    {
        if (mismatchedTargets.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"На платформах {string.Join(", ", mismatchedTargets)} отключены другие модули, чем на текущей. " +
                "«Применить» запишет текущий набор во все платформы.",
                MessageType.Warning);
        }

        var unknownIds = pendingDisabled.Where(id => modules.All(module => module.Id != id)).ToList();
        if (unknownIds.Count == 0)
            return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(
            $"Отключены модули, которых нет в проекте: {string.Join(", ", unknownIds)}.",
            MessageType.Warning);

        if (GUILayout.Button("Убрать", GUILayout.Width(70f), GUILayout.Height(38f)))
            pendingDisabled.ExceptWith(unknownIds);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLayer(PRModuleLayer layer, string title, Action drawBeforeGroups)
    {
        if (!DrawSectionHeader(layer.ToString(), title, EditorStyles.foldoutHeader))
            return;

        EditorGUI.indentLevel++;
        drawBeforeGroups?.Invoke();

        foreach (PRModuleGroup group in Enum.GetValues(typeof(PRModuleGroup)))
        {
            if (group == PRModuleGroup.Integrations)
                continue;

            var groupModules = modules
                .Where(module => module.Layer == layer && module.Manifest.Group == group && MatchesSearch(module))
                .ToList();

            if (groupModules.Count == 0)
                continue;

            if (!DrawSectionHeader($"{layer}/{group}", $"{GetGroupTitle(group)} ({groupModules.Count})", EditorStyles.foldout))
                continue;

            foreach (PRModuleInfo module in groupModules)
                DrawModule(module);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    private void DrawIntegrations()
    {
        var integrations = modules.Where(module => module.Manifest.Group == PRModuleGroup.Integrations).ToList();

        if (integrations.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Интеграций в проекте нет. Интеграция — модуль, у которого в манифесте выбрана группа «Интеграции».",
                MessageType.Info);
            return;
        }

        var slots = integrations
            .Where(module => module.Slot.Length > 0)
            .GroupBy(module => module.Slot, StringComparer.OrdinalIgnoreCase)
            .OrderBy(slot => slot.Key, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string, PRModuleInfo> slot in slots)
            DrawSlot(slot.Key, slot.ToList());

        var withoutSlot = integrations.Where(module => module.Slot.Length == 0 && MatchesSearch(module)).ToList();
        if (withoutSlot.Count == 0)
            return;

        if (!DrawSectionHeader("Integrations/NoSlot", $"Без слота ({withoutSlot.Count})", EditorStyles.foldoutHeader))
            return;

        foreach (PRModuleInfo module in withoutSlot)
            DrawModule(module);
    }

    private void DrawSlot(string slot, List<PRModuleInfo> slotModules)
    {
        if (!DrawSectionHeader("Slot/" + slot, $"{slot} — включается один модуль", EditorStyles.foldoutHeader))
            return;

        var enabledModules = slotModules.Where(module => !pendingDisabled.Contains(module.Id)).ToList();

        if (enabledModules.Count > 1)
        {
            EditorGUILayout.HelpBox(
                $"В слоте «{slot}» включено несколько модулей: {JoinNames(enabledModules)}. Оставьте один.",
                MessageType.Error);
        }

        bool nothingEnabled = enabledModules.Count == 0;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        if (DrawSwitch("Не подключено", nothingEnabled, true) && !nothingEnabled)
        {
            foreach (PRModuleInfo module in enabledModules)
            {
                if (!TryDisable(module))
                    break;
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Модули слота в сборку не входят, SDK работает на своих реализациях по умолчанию.", wrappedMiniLabel);
        EditorGUILayout.EndVertical();

        foreach (PRModuleInfo module in slotModules.Where(MatchesSearch))
            DrawModule(module, true);

        EditorGUILayout.Space();
    }

    private bool DrawSwitch(string label, bool value, bool radio)
    {
        if (!radio)
            return EditorGUILayout.ToggleLeft(label, value, EditorStyles.boldLabel);

        // GUILayout отступ EditorGUI.indentLevel не учитывает.
        GUILayout.Space(EditorGUI.indentLevel * 15f);
        return GUILayout.Toggle(value, " " + label, radioLabel);
    }

    private void DrawCore()
    {
        if (!DrawSectionHeader("Core", "Ядро — не отключается", EditorStyles.foldout))
            return;

        EditorGUILayout.LabelField(string.Join(" · ", coreParts), wrappedMiniLabel);
    }

    private bool DrawSectionHeader(string key, string title, GUIStyle style)
    {
        bool expanded = !collapsedSections.Contains(key);
        bool next = style == EditorStyles.foldoutHeader
            ? EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title)
            : EditorGUILayout.Foldout(expanded, title, true);

        if (style == EditorStyles.foldoutHeader)
            EditorGUILayout.EndFoldoutHeaderGroup();

        if (next)
            collapsedSections.Remove(key);
        else
            collapsedSections.Add(key);

        return next;
    }

    private void DrawModule(PRModuleInfo module, bool radio = false)
    {
        bool enabled = !pendingDisabled.Contains(module.Id);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(module.IsRequired && enabled))
        {
            bool next = DrawSwitch(module.DisplayName, enabled, radio);

            // Переключатель слота снимается выбором другого модуля или «Не подключено», а не повторным нажатием.
            if (next != enabled && (next || !radio))
                SetEnabled(module, next);
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label(GetStateText(module, enabled), EditorStyles.miniLabel);

        if (GUILayout.Button("Манифест", EditorStyles.miniButton, GUILayout.Width(70f)))
        {
            Selection.activeObject = module.Manifest;
            EditorGUIUtility.PingObject(module.Manifest);
        }

        EditorGUILayout.EndHorizontal();

        string info = $"{module.Id} · {module.Folder} · скриптов: {module.Scripts.Count}";
        EditorGUILayout.LabelField(info, wrappedMiniLabel);

        if (module.PartFolders.Count > 0)
            EditorGUILayout.LabelField("Ещё папки: " + string.Join(", ", module.PartFolders), wrappedMiniLabel);

        if (!string.IsNullOrWhiteSpace(module.Manifest.Description))
            EditorGUILayout.LabelField(module.Manifest.Description, wrappedMiniLabel);

        if (module.Dependencies.Count > 0)
            EditorGUILayout.LabelField("Зависит от: " + JoinNames(module.Dependencies), wrappedMiniLabel);

        if (module.Dependents.Count > 0)
            EditorGUILayout.LabelField("Нужен для: " + JoinNames(module.Dependents), wrappedMiniLabel);

        DrawPlugin(module);
        DrawModuleProblems(module, enabled);
        EditorGUILayout.EndVertical();
    }

    private void DrawPlugin(PRModuleInfo module)
    {
        string folder = module.Manifest.PluginFolder;
        if (folder.Length > 0)
        {
            string state = missingPlugins.Contains(module) ? "не найден" : "есть в проекте";
            EditorGUILayout.LabelField($"Плагин: {folder} — {state}", wrappedMiniLabel);
        }

        if (!pluginModules.TryGetValue(module, out List<string> parts))
            return;

        if (!DrawSectionHeader("Plugin/" + module.Folder, $"Модули плагина ({parts.Count})", EditorStyles.foldout))
            return;

        EditorGUI.indentLevel++;

        foreach (string part in parts)
            EditorGUILayout.LabelField(part, EditorStyles.miniLabel);

        EditorGUI.indentLevel--;
    }

    private void DrawModuleProblems(PRModuleInfo module, bool enabled)
    {
        if (!PRModuleManifest.IsValidId(module.Id))
            EditorGUILayout.HelpBox("Идентификатор пуст или содержит что-то кроме A–Z, 0–9 и _.", MessageType.Error);

        if (duplicateIds.Contains(module.Id))
            EditorGUILayout.HelpBox($"Идентификатор {module.Id} занят другим модулем.", MessageType.Error);

        if (module.MissingDependencyCount > 0)
            EditorGUILayout.HelpBox("В манифесте есть пустые ссылки на зависимости.", MessageType.Warning);

        if (module.IsRequired && !enabled)
            EditorGUILayout.HelpBox("Модуль обязательный, но отключён. Включите его.", MessageType.Error);

        if (enabled && missingPlugins.Contains(module))
        {
            EditorGUILayout.HelpBox(
                "Плагина нет в проекте, а модуль включён: проект не соберётся. Установите плагин или отключите модуль.",
                MessageType.Error);
        }

        if (module.UnguardedScripts.Count == 0)
            return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(
            $"Файлов без обёртки модуля: {module.UnguardedScripts.Count}. Пока они есть, модуль нельзя отключить.",
            MessageType.Warning);

        if (GUILayout.Button("Обернуть", GUILayout.Width(80f), GUILayout.Height(38f)))
            WrapScripts(module);

        EditorGUILayout.EndHorizontal();
    }

    private string GetStateText(PRModuleInfo module, bool enabled)
    {
        if (module.IsRequired)
            return "обязательный";

        bool applied = !appliedDisabled.Contains(module.Id);
        if (applied == enabled)
            return enabled ? "включён" : "отключён";

        return enabled ? "будет включён" : "будет отключён";
    }

    private void SetEnabled(PRModuleInfo module, bool enable)
    {
        if (enable)
            TryEnable(module);
        else
            TryDisable(module);
    }

    private bool TryEnable(PRModuleInfo module)
    {
        var disabledDependencies = PRModuleCatalog.CollectDependencies(module)
            .Where(dependency => pendingDisabled.Contains(dependency.Id))
            .ToList();

        if (disabledDependencies.Count > 0 && !EditorUtility.DisplayDialog(
                "Нужны зависимости",
                $"«{module.DisplayName}» не компилируется без: {JoinNames(disabledDependencies)}.\n\nВключить их тоже?",
                "Включить", "Отмена"))
            return false;

        // Соседи по слоту отключаются без вопроса: выбор одного модуля слота и есть отказ от остальных.
        foreach (PRModuleInfo rival in GetSlotRivals(module).Where(rival => !pendingDisabled.Contains(rival.Id)).ToList())
        {
            if (!TryDisable(rival))
                return false;
        }

        foreach (PRModuleInfo dependency in disabledDependencies)
            pendingDisabled.Remove(dependency.Id);

        pendingDisabled.Remove(module.Id);
        return true;
    }

    private IEnumerable<PRModuleInfo> GetSlotRivals(PRModuleInfo module)
    {
        return module.Slot.Length == 0
            ? Enumerable.Empty<PRModuleInfo>()
            : modules.Where(other => other != module && string.Equals(other.Slot, module.Slot, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryDisable(PRModuleInfo module)
    {
        var blockers = module.Dependents.Where(dependent => !pendingDisabled.Contains(dependent.Id)).ToList();
        if (blockers.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Нельзя отключить",
                $"«{module.DisplayName}» нужен включённым модулям: {JoinNames(blockers)}.\n\nСначала отключите их.",
                "Понятно");
            return false;
        }

        if (!PRModuleManifest.IsValidId(module.Id) || duplicateIds.Contains(module.Id))
        {
            EditorUtility.DisplayDialog("Нельзя отключить", "Сначала исправьте идентификатор в манифесте.", "Понятно");
            return false;
        }

        if (module.UnguardedScripts.Count > 0)
        {
            if (!EditorUtility.DisplayDialog(
                    "Файлы без обёртки",
                    $"У «{module.DisplayName}» файлов без обёртки: {module.UnguardedScripts.Count}. " +
                    "Они останутся в сборке и сломают компиляцию.\n\nОбернуть их сейчас?",
                    "Обернуть", "Отмена"))
                return false;

            if (!WrapScripts(module))
                return false;
        }

        pendingDisabled.Add(module.Id);
        return true;
    }

    private bool WrapScripts(PRModuleInfo module)
    {
        List<string> failed = PRModuleGuard.WrapAll(module.UnguardedScripts, module.Id);
        Reload(true);

        if (failed.Count == 0)
            return true;

        EditorUtility.DisplayDialog(
            "Не все файлы обёрнуты",
            "Эти файлы не в UTF-8, их нужно перекодировать и обернуть заново:\n\n" + string.Join("\n", failed),
            "Понятно");
        return false;
    }

    private void Apply()
    {
        var toDisable = modules.Where(module => pendingDisabled.Contains(module.Id) && !appliedDisabled.Contains(module.Id)).ToList();
        var toEnable = modules.Where(module => appliedDisabled.Contains(module.Id) && !pendingDisabled.Contains(module.Id)).ToList();

        if (toDisable.Any(module => module.UnguardedScripts.Count > 0))
        {
            EditorUtility.DisplayDialog("Нельзя применить", "У отключаемых модулей остались файлы без обёртки.", "Понятно");
            return;
        }

        IGrouping<string, PRModuleInfo> crowdedSlot = modules
            .Where(module => module.Slot.Length > 0 && !pendingDisabled.Contains(module.Id))
            .GroupBy(module => module.Slot, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(slot => slot.Count() > 1);

        if (crowdedSlot != null)
        {
            EditorUtility.DisplayDialog(
                "Нельзя применить",
                $"В слоте «{crowdedSlot.Key}» включено несколько модулей: {JoinNames(crowdedSlot)}. Оставьте один.",
                "Понятно");
            return;
        }

        string message = string.Empty;
        if (toDisable.Count > 0)
            message += "Отключить: " + JoinNames(toDisable) + "\n";
        if (toEnable.Count > 0)
            message += "Включить: " + JoinNames(toEnable) + "\n";

        if (toDisable.Count > 0)
        {
            message += "\nПока модуль отключён:\n" +
                "• его компоненты на сценах и префабах станут Missing Script, такие префабы лучше не пересохранять;\n" +
                "• поля, которые он добавляет в настройки и базу SDK, пропадут при сохранении этих ассетов;\n" +
                "• его данные в сохранениях игроков пропадут при первом сохранении.\n";
        }

        message += "\nПосле применения проект пересоберётся.";

        if (!EditorUtility.DisplayDialog("Применить модули", message, "Применить", "Отмена"))
            return;

        AssetDatabase.SaveAssets();
        PRModuleDefines.WriteDisabledIds(pendingDisabled);
        Reload(true);
    }

    private bool MatchesSearch(PRModuleInfo module)
    {
        return string.IsNullOrWhiteSpace(search)
            || module.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || (module.Id ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string JoinNames(IEnumerable<PRModuleInfo> list)
    {
        return string.Join(", ", list.Select(module => module.DisplayName));
    }

    private static string GetGroupTitle(PRModuleGroup group)
    {
        return group switch
        {
            PRModuleGroup.Modules => "Модули",
            PRModuleGroup.Windows => "Окна",
            PRModuleGroup.Components => "Компоненты",
            PRModuleGroup.Entities => "Сущности",
            PRModuleGroup.Integrations => "Интеграции",
            _ => "Прочее"
        };
    }
}
