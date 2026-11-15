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

    /// <summary>
    /// Что не так с плагинами и шаблоном при применённом наборе: их правили руками или модуль только что получил флаг.
    /// </summary>
    private List<string> pluginMismatches = new();

    private readonly HashSet<string> collapsedSections = new();

    private string search = string.Empty;

    // Сериализуется, чтобы после «Применить» и пересборки окно осталось на той же вкладке.
    [SerializeField] private int tab;

    private Vector2 scroll;

    private GUIStyle wrappedMiniLabel;

    [MenuItem(MenuPath, false, 1)]
    private static void Open()
    {
        var window = GetWindow<PRModulesWindow>();
        window.titleContent = new GUIContent("Модули");
        window.minSize = new Vector2(760f, 420f);
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

        // Спрятанный плагин отсутствующим не считается, если модуль умеет его возвращать: это сделает «Применить».
        missingPlugins = new HashSet<PRModuleInfo>(modules.Where(module =>
            PRModulePlugins.GetState(module.Manifest) switch
            {
                PRPluginState.Missing => true,
                PRPluginState.Hidden => !module.Manifest.HidePluginWhenDisabled,
                _ => false
            }));

        pluginMismatches = PRModulePlugins.DescribeChanges(modules, appliedDisabled);
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
        EnsureStyles();

        CreateHorizontalToolBar(DrawToolbar);
        DrawProjectWarnings();
        DrawUnguardedWarning();

        List<ListEntry> entries = tab == IntegrationsTab ? BuildIntegrationEntries() : BuildModuleEntries();
        ListEntry selected = ResolveSelected(entries);

        // Слева список с галками, справа один модуль целиком. Карточки с описанием,
        // папкой и предупреждениями у каждого модуля умещали на экране пять модулей из сорока.
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
        {
            using (var list = new EditorGUILayout.VerticalScope(
                       GUILayout.Width(ListWidth),
                       GUILayout.ExpandHeight(true)))
            {
                PRSDKSidebarGUI.Background(list.rect);
                tab = GUILayout.Toolbar(tab, TabTitles);

                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

                foreach (ListEntry entry in entries)
                    DrawEntry(entry, entry == selected);

                if (entries.Count == 0)
                    GUILayout.Label("Ничего не найдено", EditorStyles.centeredGreyMiniLabel);

                EditorGUILayout.EndScrollView();
                DrawLegend();
            }

            PRSDKSidebarGUI.Separator();

            detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll);

            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins))
            {
                if (selected == null)
                    EditorGUILayout.HelpBox("Выберите модуль слева.", MessageType.Info);
                else if (selected.Kind == EntryKind.Core)
                    DrawCoreDetails();
                else
                    DrawModuleDetails(selected.Module);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    #region Список слева

    private const float ListWidth = 320f;

    private const string CoreKey = "core";

    private enum EntryKind
    {
        Header,
        SubHeader,
        Note,
        Core,
        Module,
        SlotNone
    }

    /// <summary>
    /// Строка списка: заголовок, пояснение, ядро, модуль или «не подключено» у слота.
    /// </summary>
    private sealed class ListEntry
    {
        public EntryKind Kind;
        public string Key;
        public string Title;
        public string Count;
        public int Depth;
        public PRModuleInfo Module;
        public bool Radio;
        public List<PRModuleInfo> SlotModules;
        public MessageType NoteType;
    }

    /// <summary>
    /// Модуль, открытый справа: его папка либо <see cref="CoreKey"/>.
    /// </summary>
    /// <remarks>
    /// Сериализуется: «Применить» пересобирает проект, и окно должно вернуться к тому же модулю.
    /// </remarks>
    [SerializeField] private string selectedKey;

    private Vector2 detailsScroll;

    // Стили не сериализуются: редактор сохранил бы старый набор через перезагрузку скриптов,
    // и добавленный позже стиль остался бы пустым при непустых остальных.
    [NonSerialized] private GUIStyle titleStyle;

    [NonSerialized] private GUIStyle wrappedLabel;

    [NonSerialized] private GUIStyle factLabel;

    private void EnsureStyles()
    {
        wrappedMiniLabel ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
        wrappedLabel = new GUIStyle(EditorStyles.label) { wordWrap = true };
        factLabel = new GUIStyle(EditorStyles.label);
        factLabel.normal.textColor = Color.Lerp(EditorStyles.label.normal.textColor, Color.gray, 0.6f);
    }

    private bool Searching => !string.IsNullOrWhiteSpace(search);

    /// <summary>
    /// Развёрнута ли группа. Во время поиска раскрыты все: свёрнутая спрятала бы найденное.
    /// </summary>
    private bool IsExpanded(string key)
    {
        return Searching || !collapsedSections.Contains(key);
    }

    private List<ListEntry> BuildModuleEntries()
    {
        var entries = new List<ListEntry>();

        if (!Searching || "ядро".IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
            entries.Add(new ListEntry { Kind = EntryKind.Core, Key = CoreKey, Title = "Ядро SDK" });

        AddLayerEntries(entries, PRModuleLayer.Public, "Публичная часть");
        AddLayerEntries(entries, PRModuleLayer.Private, "Приватная часть");
        AddLayerEntries(entries, PRModuleLayer.Project, "Проект");

        return entries;
    }

    /// <summary>
    /// Добавляет группы одного слоя: «Приватная часть · Окна» и так далее.
    /// </summary>
    /// <remarks>
    /// Слой и группа стоят в одном заголовке: два уровня стрелок ради десятка строк
    /// только добавляли щелчков.
    /// </remarks>
    private void AddLayerEntries(List<ListEntry> entries, PRModuleLayer layer, string layerTitle)
    {
        foreach (PRModuleGroup group in Enum.GetValues(typeof(PRModuleGroup)))
        {
            if (group == PRModuleGroup.Integrations)
                continue;

            var groupModules = modules
                .Where(module => module.Layer == layer && module.Manifest.Group == group && MatchesSearch(module))
                .ToList();

            if (groupModules.Count == 0)
                continue;

            string key = $"{layer}/{group}";
            entries.Add(new ListEntry
            {
                Kind = EntryKind.Header,
                Key = key,
                Title = $"{layerTitle} · {GetGroupTitle(group)}",
                Count = groupModules.Count.ToString()
            });

            if (!IsExpanded(key))
                continue;

            foreach (PRModuleInfo module in groupModules)
                entries.Add(new ListEntry { Kind = EntryKind.Module, Key = module.Folder, Module = module, Depth = 1 });
        }
    }

    private List<ListEntry> BuildIntegrationEntries()
    {
        var entries = new List<ListEntry>();
        var integrations = modules.Where(module => module.Manifest.Group == PRModuleGroup.Integrations).ToList();

        if (integrations.Count == 0)
        {
            entries.Add(new ListEntry
            {
                Kind = EntryKind.Note,
                Title = "Интеграций в проекте нет. Интеграция — модуль, у которого в манифесте выбрана группа «Интеграции».",
                NoteType = MessageType.Info
            });
            return entries;
        }

        // Сначала платформы по имени, интеграции для любой платформы — последними.
        var platforms = integrations
            .GroupBy(module => module.Manifest.Platform, StringComparer.OrdinalIgnoreCase)
            .OrderBy(platform => platform.Key.Length == 0)
            .ThenBy(platform => platform.Key, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string, PRModuleInfo> platform in platforms)
            AddPlatformEntries(entries, platform.Key, platform.ToList());

        return entries;
    }

    /// <summary>
    /// Интеграции одной платформы сборки: её слоты и модули без слота.
    /// </summary>
    private void AddPlatformEntries(List<ListEntry> entries, string platform, List<PRModuleInfo> platformModules)
    {
        var visible = platformModules.Where(MatchesSearch).ToList();

        if (visible.Count == 0)
            return;

        string key = "Platform/" + platform;
        entries.Add(new ListEntry
        {
            Kind = EntryKind.Header,
            Key = key,
            Title = platform.Length == 0 ? "Любая платформа" : GetPlatformTitle(platform),
            Count = visible.Count.ToString()
        });

        if (!IsExpanded(key))
            return;

        BuildTarget active = EditorUserBuildSettings.activeBuildTarget;
        if (platform.Length > 0 && !platformModules[0].Manifest.SupportsPlatform(active))
        {
            bool anyEnabled = platformModules.Any(module => !pendingDisabled.Contains(module.Id));
            entries.Add(new ListEntry
            {
                Kind = EntryKind.Note,
                Title = $"Активная платформа сборки — {active}. Эти интеграции работают только в {platform}" +
                        (anyEnabled ? ": включённая не попадёт в сборку как надо, переключите платформу (File → Build Settings)." : "."),
                NoteType = anyEnabled ? MessageType.Warning : MessageType.Info
            });
        }

        var slots = platformModules
            .Where(module => module.Slot.Length > 0)
            .GroupBy(module => module.Slot, StringComparer.OrdinalIgnoreCase)
            .OrderBy(slot => slot.Key, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string, PRModuleInfo> slot in slots)
        {
            List<PRModuleInfo> slotModules = slot.ToList();
            List<PRModuleInfo> slotVisible = slotModules.Where(MatchesSearch).ToList();

            if (slotVisible.Count == 0)
                continue;

            entries.Add(new ListEntry { Kind = EntryKind.SubHeader, Title = $"{slot.Key} — один на выбор", Depth = 1 });

            var enabledModules = slotModules.Where(module => !pendingDisabled.Contains(module.Id)).ToList();
            if (enabledModules.Count > 1)
            {
                entries.Add(new ListEntry
                {
                    Kind = EntryKind.Note,
                    Title = $"В слоте «{slot.Key}» включено несколько модулей: {JoinNames(enabledModules)}. Оставьте один.",
                    NoteType = MessageType.Error
                });
            }

            entries.Add(new ListEntry
            {
                Kind = EntryKind.SlotNone,
                Title = "Не подключено",
                Depth = 1,
                SlotModules = slotModules
            });

            foreach (PRModuleInfo module in slotVisible)
            {
                entries.Add(new ListEntry
                {
                    Kind = EntryKind.Module,
                    Key = module.Folder,
                    Module = module,
                    Depth = 1,
                    Radio = true
                });
            }
        }

        var withoutSlot = visible.Where(module => module.Slot.Length == 0).ToList();
        if (withoutSlot.Count == 0)
            return;

        entries.Add(new ListEntry { Kind = EntryKind.SubHeader, Title = "Без слота", Depth = 1 });

        foreach (PRModuleInfo module in withoutSlot)
            entries.Add(new ListEntry { Kind = EntryKind.Module, Key = module.Folder, Module = module, Depth = 1 });
    }

    /// <summary>
    /// Находит открытую строку, а если её нет в списке — берёт первый модуль.
    /// </summary>
    private ListEntry ResolveSelected(List<ListEntry> entries)
    {
        ListEntry fallback = null;

        foreach (ListEntry entry in entries)
        {
            if (entry.Kind != EntryKind.Module && entry.Kind != EntryKind.Core)
                continue;

            if (entry.Key == selectedKey)
                return entry;

            // Ядро первым не выбирается: оно не настраивается, а окно открывают ради модулей.
            if (fallback == null || fallback.Kind == EntryKind.Core)
                fallback = entry;
        }

        // Выбор переписывается, только когда выбранного модуля больше нет в проекте.
        // Поиск, другая вкладка и свёрнутая группа лишь прячут его строку: убрали запрос
        // или раскрыли группу — окно возвращается к тому же модулю.
        if (fallback != null && !IsKnownKey(selectedKey))
            selectedKey = fallback.Key;

        return fallback;
    }

    private bool IsKnownKey(string key)
    {
        return key == CoreKey || modules.Any(module => module.Folder == key);
    }

    private void DrawEntry(ListEntry entry, bool selected)
    {
        switch (entry.Kind)
        {
            case EntryKind.Header:
                bool expanded = IsExpanded(entry.Key);
                bool next = PRSDKSidebarGUI.FoldoutHeader(entry.Title, expanded, entry.Count);

                if (!Searching && next != expanded)
                {
                    if (next)
                        collapsedSections.Remove(entry.Key);
                    else
                        collapsedSections.Add(entry.Key);
                }

                break;

            case EntryKind.SubHeader:
                PRSDKSidebarGUI.Header(entry.Title, entry.Depth);
                break;

            case EntryKind.Note:
                EditorGUILayout.HelpBox(entry.Title, entry.NoteType);
                break;

            case EntryKind.Core:
                if (PRSDKSidebarGUI.ToggleRow(
                        entry.Title, 0, selected, true, out _,
                        toggleDisabled: true,
                        tooltip: "Ядро не отключается: на нём держатся все модули."))
                {
                    Select(entry.Key);
                }

                break;

            case EntryKind.SlotNone:
                DrawSlotNone(entry);
                break;

            case EntryKind.Module:
                DrawModuleRow(entry, selected);
                break;
        }
    }

    private void DrawSlotNone(ListEntry entry)
    {
        var enabledModules = entry.SlotModules.Where(module => !pendingDisabled.Contains(module.Id)).ToList();
        bool nothingEnabled = enabledModules.Count == 0;

        // У этой строки нечего открывать справа, поэтому щелчок по названию — тот же выбор,
        // что и щелчок по переключателю.
        bool clicked = PRSDKSidebarGUI.ToggleRow(
            entry.Title, entry.Depth, false, nothingEnabled, out bool next,
            radio: true,
            dimmed: !nothingEnabled,
            tooltip: "Модули слота в сборку не входят, SDK работает на своих реализациях по умолчанию.");

        if (nothingEnabled || !next && !clicked)
            return;

        foreach (PRModuleInfo module in enabledModules)
        {
            if (!TryDisable(module))
                break;
        }

        GUIUtility.ExitGUI();
    }

    private void DrawModuleRow(ListEntry entry, bool selected)
    {
        PRModuleInfo module = entry.Module;
        bool enabled = !pendingDisabled.Contains(module.Id);
        bool applied = !appliedDisabled.Contains(module.Id);
        string problem = GetProblemSummary(module, enabled, out bool isError);

        string tooltip = GetStateText(module, enabled);
        if (problem != null)
            tooltip += "\n" + problem;

        Texture icon = problem == null
            ? null
            : EditorGUIUtility.IconContent(isError ? "console.erroricon.sml" : "console.warnicon.sml").image;

        bool clicked = PRSDKSidebarGUI.ToggleRow(
            module.DisplayName, entry.Depth, selected, enabled, out bool next,
            radio: entry.Radio,
            toggleDisabled: module.IsRequired && enabled,
            dimmed: !enabled,
            icon: icon,
            changed: applied != enabled,
            tooltip: tooltip);

        if (clicked)
            Select(entry.Key);

        // Переключатель слота снимается выбором другого модуля или «Не подключено», а не повторным нажатием.
        if (next == enabled || !next && entry.Radio)
            return;

        SetEnabled(module, next);

        // Переключение могло обернуть файлы и перечитать модули: строки этого кадра устарели.
        GUIUtility.ExitGUI();
    }

    private void Select(string key)
    {
        selectedKey = key;
        detailsScroll = Vector2.zero;
        GUI.FocusControl(null);
        Repaint();
    }

    /// <summary>
    /// Открывает модуль справа, где бы он ни лежал: на другой вкладке или в свёрнутой группе.
    /// </summary>
    private void JumpTo(PRModuleInfo module)
    {
        tab = module.Manifest.Group == PRModuleGroup.Integrations ? IntegrationsTab : 0;
        collapsedSections.Clear();
        search = string.Empty;
        Select(module.Folder);
    }

    /// <summary>
    /// Подпись под списком: что значат значки в строках.
    /// </summary>
    private void DrawLegend()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(8f);
            Rect dot = GUILayoutUtility.GetRect(7f, 16f, GUILayout.Width(7f));

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(dot.x, dot.y + 5f, 7f, 7f), new Color(0.35f, 0.65f, 1f, 1f));

            GUILayout.Label("изменено, ждёт «Применить»", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(2f);
    }

    /// <summary>
    /// Коротко о том, что не так с модулем, — для значка и подсказки в списке.
    /// </summary>
    /// <returns>Текст либо <c>null</c>, если замечаний нет.</returns>
    private string GetProblemSummary(PRModuleInfo module, bool enabled, out bool isError)
    {
        var lines = new List<string>();
        isError = false;

        if (!PRModuleManifest.IsValidId(module.Id))
        {
            lines.Add("Неверный идентификатор.");
            isError = true;
        }

        if (duplicateIds.Contains(module.Id))
        {
            lines.Add("Идентификатор занят другим модулем.");
            isError = true;
        }

        if (module.IsRequired && !enabled)
        {
            lines.Add("Обязательный модуль отключён.");
            isError = true;
        }

        if (enabled && missingPlugins.Contains(module))
        {
            lines.Add("Плагина нет в проекте.");
            isError = true;
        }

        if (module.MissingDependencyCount > 0)
            lines.Add("Пустые ссылки на зависимости.");

        if (module.UnguardedScripts.Count > 0)
            lines.Add($"Файлов без обёртки: {module.UnguardedScripts.Count}.");

        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    #endregion

    #region Модуль справа

    private void DrawCoreDetails()
    {
        GUILayout.Label("Ядро SDK", titleStyle);
        GUILayout.Label("Не отключается: на нём держатся все модули.", wrappedLabel);
        EditorGUILayout.Space(6f);

        DrawFact("Папка", CoreFolder);
        DrawFact("Частей", coreParts.Count.ToString());
        EditorGUILayout.Space(6f);
        GUILayout.Label(string.Join(" · ", coreParts), wrappedLabel);
    }

    private void DrawModuleDetails(PRModuleInfo module)
    {
        bool enabled = !pendingDisabled.Contains(module.Id);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(module.DisplayName, titleStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Манифест", EditorStyles.miniButton, GUILayout.Width(80f)))
            {
                Selection.activeObject = module.Manifest;
                EditorGUIUtility.PingObject(module.Manifest);
            }
        }

        if (!string.IsNullOrWhiteSpace(module.Manifest.Description))
            GUILayout.Label(module.Manifest.Description, wrappedLabel);

        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(module.IsRequired && enabled))
            {
                bool next = EditorGUILayout.ToggleLeft("В сборке", enabled, EditorStyles.boldLabel, GUILayout.Width(100f));

                if (next != enabled)
                {
                    SetEnabled(module, next);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.Label(GetStateText(module, enabled), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(6f);
        DrawModuleProblems(module, enabled);

        DrawFact("Идентификатор", module.Id);

        if (module.Manifest.Platform.Length > 0)
            DrawFact("Платформа", module.Manifest.Platform);

        if (module.Slot.Length > 0)
            DrawFact("Слот", $"{module.Slot} — включается один модуль");

        DrawFact("Папка", module.Folder);

        if (module.PartFolders.Count > 0)
            DrawFact("Ещё папки", string.Join(", ", module.PartFolders));

        DrawFact("Скриптов", module.Scripts.Count.ToString());

        DrawLinks("Зависит от", module.Dependencies);
        DrawLinks("Нужен для", module.Dependents);
        DrawPlugin(module);
    }

    /// <summary>
    /// Строка «подпись — значение»; длинное значение переносится.
    /// </summary>
    private void DrawFact(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, factLabel, GUILayout.Width(FactLabelWidth));
            GUILayout.Label(value, wrappedLabel);
        }
    }

    private const float FactLabelWidth = 120f;

    /// <summary>
    /// Связанные модули кнопками: щелчок открывает модуль, чтобы не искать его в списке.
    /// </summary>
    private void DrawLinks(string label, IEnumerable<PRModuleInfo> linked)
    {
        List<PRModuleInfo> list = linked.ToList();

        if (list.Count == 0)
            return;

        const int perRow = 3;

        for (int first = 0; first < list.Count; first += perRow)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(first == 0 ? label : string.Empty, factLabel, GUILayout.Width(FactLabelWidth));

                for (int index = first; index < list.Count && index < first + perRow; index++)
                {
                    PRModuleInfo module = list[index];
                    bool enabled = !pendingDisabled.Contains(module.Id);
                    var content = new GUIContent(module.DisplayName, enabled ? "включён" : "отключён");

                    if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.MaxWidth(220f)))
                    {
                        JumpTo(module);
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }

    #endregion

    private void DrawToolbar()
    {
        // Поиск стоит над списком, который он отбирает.
        search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(ListWidth - 8f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            Reload(true);

        int changes = pendingDisabled.Count(id => !appliedDisabled.Contains(id))
            + appliedDisabled.Count(id => !pendingDisabled.Contains(id));

        using (new EditorGUI.DisabledScope(changes == 0))
        {
            if (GUILayout.Button("Сбросить", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                pendingDisabled = new HashSet<string>(appliedDisabled);
        }

        using (new EditorGUI.DisabledScope(changes == 0 && mismatchedTargets.Count == 0 && pluginMismatches.Count == 0))
        {
            string title = changes > 0 ? $"Применить ({changes})" : "Применить";
            if (GUILayout.Button(title, EditorStyles.toolbarButton, GUILayout.Width(100f)))
                Apply();
        }
    }

    /// <summary>
    /// Одно предупреждение на все модули с файлами без обёртки.
    /// </summary>
    /// <remarks>
    /// Одинаковая жёлтая плашка с кнопкой стояла в карточке каждого второго модуля.
    /// У одного модуля она остаётся в его описании справа.
    /// </remarks>
    private void DrawUnguardedWarning()
    {
        var unguarded = modules.Where(module => module.UnguardedScripts.Count > 0).ToList();

        if (unguarded.Count < 2)
            return;

        int files = unguarded.Sum(module => module.UnguardedScripts.Count);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(
            $"Файлов без обёртки модуля: {files}, модулей с ними: {unguarded.Count}. " +
            "Пока они есть, эти модули нельзя отключить. В списке они помечены значком.",
            MessageType.Warning);

        bool wrap = GUILayout.Button("Обернуть все", GUILayout.Width(100f), GUILayout.Height(38f));
        EditorGUILayout.EndHorizontal();

        if (!wrap)
            return;

        if (!EditorUtility.DisplayDialog(
                "Обернуть файлы",
                $"В файлы ({files}) будет дописана обёртка их модулей: {JoinNames(unguarded)}.\n\n" +
                "После этого проект пересоберётся.",
                "Обернуть", "Отмена"))
            return;

        var failed = new List<string>();

        // Файлы оборачиваются по одному, а проект обновляется один раз в конце: WrapAll
        // обновлял бы его после каждого модуля и запускал промежуточные пересборки.
        foreach (PRModuleInfo module in unguarded)
        {
            foreach (string path in module.UnguardedScripts.ToList())
            {
                if (!PRModuleGuard.Wrap(path, module.Id))
                    failed.Add(path);
            }
        }

        AssetDatabase.Refresh();
        Reload(true);

        if (failed.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Не все файлы обёрнуты",
                "Эти файлы не в UTF-8, их нужно перекодировать и обернуть заново:\n\n" + string.Join("\n", failed),
                "Понятно");
        }

        GUIUtility.ExitGUI();
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

        if (pluginMismatches.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "Плагины и шаблон WebGL не соответствуют включённым модулям. «Применить» исправит: " +
                string.Join("; ", pluginMismatches) + ".",
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

    /// <summary>
    /// Заголовок группы платформы: для браузера — с пояснением, что это площадки веб-игр.
    /// </summary>
    private static string GetPlatformTitle(string platform)
    {
        if (string.Equals(platform, BuildTarget.WebGL.ToString(), StringComparison.OrdinalIgnoreCase))
            return "WebGL — браузерные площадки";

        if (string.Equals(platform, BuildTarget.Android.ToString(), StringComparison.OrdinalIgnoreCase))
            return "Android — магазины и мобильная реклама";

        return platform.IndexOf("Standalone", StringComparison.OrdinalIgnoreCase) >= 0
            ? "PC — Windows, Linux, macOS"
            : platform;
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

    private void DrawPlugin(PRModuleInfo module)
    {
        string folder = module.Manifest.PluginFolder;
        if (folder.Length > 0)
        {
            string state = PRModulePlugins.GetState(module.Manifest) switch
            {
                PRPluginState.Visible => "есть в проекте",
                PRPluginState.Hidden => $"спрятан в {folder}{PRModulePlugins.HiddenSuffix}",
                _ => "не найден"
            };

            if (module.Manifest.HidePluginWhenDisabled)
                state += ", прячется, пока модуль отключён";
            DrawFact("Плагин", $"{folder} — {state}");
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

        if (enabled && module.Manifest.HidePluginWhenDisabled && PRModulePlugins.GetState(module.Manifest) == PRPluginState.Hidden)
            EditorGUILayout.HelpBox("Плагин спрятан. «Применить» вернёт его на место.", MessageType.Info);

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
            : modules.Where(other => other != module && string.Equals(other.SlotKey, module.SlotKey, StringComparison.OrdinalIgnoreCase));
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
            .GroupBy(module => module.SlotKey, StringComparer.OrdinalIgnoreCase)
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

        List<string> pluginChanges = PRModulePlugins.DescribeChanges(modules, pendingDisabled);
        if (pluginChanges.Count > 0)
            message += "\nПлагины и шаблон: " + string.Join("; ", pluginChanges) + ".\n";

        message += "\nПосле применения проект пересоберётся.";

        if (!EditorUtility.DisplayDialog("Применить модули", message, "Применить", "Отмена"))
            return;

        AssetDatabase.SaveAssets();

        // Сначала вернуть плагины включаемых модулей: без плагина модуль не соберётся, и символы тогда не пишутся.
        List<string> showErrors = PRModulePlugins.ShowEnabled(modules, pendingDisabled);
        if (showErrors.Count > 0)
        {
            AssetDatabase.Refresh();
            Reload(true);
            EditorUtility.DisplayDialog(
                "Плагин не вернулся",
                "Модули не переключены: без плагина включаемый модуль не соберётся.\n\n" + string.Join("\n\n", showErrors),
                "Понятно");
            return;
        }

        PRModuleDefines.WriteDisabledIds(pendingDisabled);

        // Плагин, который не удалось спрятать, сборке не мешает, но попадёт в страницу; без исходника шаблона
        // остановится сборка WebGL. Об этом надо сказать.
        List<string> hideErrors = PRModulePlugins.HideDisabledAndSetTemplate(modules, pendingDisabled);
        AssetDatabase.Refresh();
        Reload(true);

        if (hideErrors.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Не всё применилось",
                "Модули переключены, но:\n\n" + string.Join("\n\n", hideErrors),
                "Понятно");
        }
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
