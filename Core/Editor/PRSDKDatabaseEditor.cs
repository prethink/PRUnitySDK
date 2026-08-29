using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отдельное окно <see cref="PRSDKDatabase"/> с управлением каталогами definitions.
/// </summary>
public abstract class PRSDKDatabaseEditor : EditorWindow
{
    private const float CardHeight = 138f;
    private const float CardWidth = 112f;
    private static readonly string[] SortLabels =
    {
        "По качеству",
        "По имени",
        "По дате добавления"
    };

    private readonly Dictionary<Type, UnityEngine.Object[]> assetCache = new();
    private readonly Dictionary<string, Vector2> detailsScrollPositions = new();
    private readonly Dictionary<string, AssetGridViewState> gridViewStates = new();
    private readonly Dictionary<string, Vector2> gridScrollPositions = new();
    private readonly Dictionary<string, UnityEngine.Object> selectedAssets = new();

    /// <summary>
    /// Выделенные карточки каждой секции.
    /// </summary>
    /// <remarks>
    /// Отдельно от <see cref="selectedAssets"/>: там лежит активная карточка, чьи свойства
    /// показаны справа, а здесь — всё, что попадёт под групповое действие.
    /// </remarks>
    private readonly Dictionary<string, HashSet<UnityEngine.Object>> selections = new();

    /// <summary>
    /// Карточка, от которой отсчитывается выделение диапазоном.
    /// </summary>
    private readonly Dictionary<string, int> selectionAnchors = new();

    /// <summary>
    /// Секция, в которой последний раз щёлкали.
    /// </summary>
    /// <remarks>
    /// Развёрнутых секций может быть несколько, а клавиша одна: без этого Delete
    /// сработал бы сразу во всех, где что-то выделено.
    /// </remarks>
    private string activeSectionKey;

    /// <summary>
    /// Дополнительные блоки модулей под инспектором выбранного ассета.
    /// </summary>
    /// <remarks>
    /// Ищутся один раз: окно не знает про магазин и другие надстройки, а они показывают
    /// свои настройки рядом с предметом — чтобы всё настраивалось в одном месте.
    /// </remarks>
    private IDatabaseAssetInspector[] assetInspectors;

    /// <summary>
    /// Метки модулей на карточках сетки.
    /// </summary>
    private IDatabaseCardBadge[] cardBadges;
    private readonly Dictionary<string, UnityEditor.Editor> selectedAssetEditors = new();
    [SerializeField] private PRSDKDatabase database;
    [SerializeField] private float gridSplit = 0.58f;
    private string search = string.Empty;
    private SerializedObject serializedDatabase;

    /// <summary>
    /// Разобранный набор, ожидающий подтверждения.
    /// </summary>
    /// <remarks>
    /// Набор применяется в два шага: сначала отчёт, потом изменение базы. Он приезжает
    /// из другой игры или ветки, где ассеты могли переехать или исчезнуть.
    /// </remarks>
    private PRSDKDatabasePresetReport pendingPreset;

    private Vector2 presetReportScroll;

    /// <summary>
    /// Что делать с тем, что уже лежит в каталогах.
    /// </summary>
    private PRSDKDatabasePresetApplyMode presetApplyMode = PRSDKDatabasePresetApplyMode.Replace;
    private Vector2 scrollPosition;
    private GUIStyle cardNameStyle;
    private GUIStyle invalidBadgeStyle;
    private bool resizeGridSplit;

    private enum AssetSortMode
    {
        Quality,
        Name,
        AddedDate
    }

    private sealed class AssetGridViewState
    {
        public AssetSortMode SortMode = AssetSortMode.AddedDate;
        public bool SortDescending;
        public int QualityFilter = -1;
        public bool InvalidOnly;

        /// <summary>
        /// Вид элемента, по которому отфильтрована сетка. Пусто — показывать все.
        /// </summary>
        public string CategoryFilter = string.Empty;
    }

    private readonly struct AssetCardEntry
    {
        public UnityEngine.Object Asset { get; }
        public int DatabaseIndex { get; }
        public bool IsInvalid { get; }

        public AssetCardEntry(
            UnityEngine.Object asset,
            int databaseIndex,
            bool isInvalid)
        {
            Asset = asset;
            DatabaseIndex = databaseIndex;
            IsInvalid = isInvalid;
        }
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("SDK Database");
        minSize = new Vector2(620f, 450f);
        BindDatabase();
    }

    private void OnDisable()
    {
        DestroyAllSelectedAssetEditors();
    }

    /// <summary>
    /// Вкладки окна помимо самой базы.
    /// </summary>
    /// <remarks>
    /// Инструменты, которые работают с тем же содержимым: генератор наград создаёт то,
    /// что потом лежит в каталоге. Держать их отдельными окнами значит заставлять
    /// переключаться между ними ради одного действия.
    /// </remarks>
    protected virtual IReadOnlyList<(string Title, Action Draw)> GetExtraTabs()
    {
        return Array.Empty<(string, Action)>();
    }

    /// <summary>
    /// Выбранная вкладка: ноль — сама база.
    /// </summary>
    private int selectedTab;

    private bool DrawTabs()
    {
        IReadOnlyList<(string Title, Action Draw)> extra = GetExtraTabs();

        if (extra.Count == 0)
            return false;

        var titles = new string[extra.Count + 1];
        titles[0] = "База";

        for (var index = 0; index < extra.Count; index++)
            titles[index + 1] = extra[index].Title;

        selectedTab = Mathf.Clamp(selectedTab, 0, titles.Length - 1);
        selectedTab = GUILayout.Toolbar(selectedTab, titles, GUILayout.Height(24f));

        if (selectedTab == 0)
            return false;

        extra[selectedTab - 1].Draw?.Invoke();
        return true;
    }

    private void OnGUI()
    {
        if (!EnsureDatabase())
        {
            EditorGUILayout.HelpBox("Не найден asset PRSDKDatabase.", MessageType.Error);
            return;
        }

        serializedDatabase.UpdateIfRequiredOrScript();
        PRSDKInspectorUtility.DrawHeader("PRUnitySDK Database", database);

        if (DrawTabs())
            return;

        DrawToolbar();
        DrawPresetReport();

        List<SerializedProperty> matched = PRSDKInspectorUtility
            .GetRootProperties(serializedDatabase)
            .Where(property =>
                IsSectionVisible(PRSDKInspectorUtility.GetFieldType(database.GetType(), property))
                && PRSDKInspectorUtility.MatchesSearch(
                    PRSDKInspectorUtility.GetSectionName(property),
                    search))
            .ToList();

        var visible = new List<SerializedProperty>();
        var links = new List<(DatabaseExternalEditorAttribute Editor, string Section)>();

        foreach (SerializedProperty property in matched)
        {
            Type fieldType = PRSDKInspectorUtility.GetFieldType(database.GetType(), property);
            DatabaseExternalEditorAttribute external = GetExternalEditor(fieldType);

            // В своём окне раздел рисуется целиком, в общем — уходит в строку ссылок.
            if (external != null && string.IsNullOrEmpty(OwnedEditorMenuPath))
                links.Add((external, PRSDKInspectorUtility.GetSectionName(property)));
            else
                visible.Add(property);
        }

        // Единственный раздел разворачивается сам: сворачивать в окне нечего, а лишний
        // клик при каждом открытии раздражает.
        bool alwaysExpanded = visible.Count == 1;

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            foreach (SerializedProperty property in visible)
                DrawSection(property, PRSDKInspectorUtility.GetSectionName(property), alwaysExpanded);

            DrawExternalLinks(links);

            if (visible.Count == 0 && links.Count == 0)
                EditorGUILayout.HelpBox("Секции с таким названием не найдены.", MessageType.Info);

            scrollPosition = scrollView.scrollPosition;
        }

        serializedDatabase.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField);

            if (GUILayout.Button("Развернуть", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                SetExpanded(true);
            if (GUILayout.Button("Свернуть", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                SetExpanded(false);
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                assetCache.Clear();
                Repaint();
            }
            if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                serializedDatabase.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("Наборы", EditorStyles.toolbarDropDown, GUILayout.Width(66f)))
                ShowPresetMenu();
            if (GUILayout.Button("Asset", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
        }
    }

    #region Наборы

    /// <summary>
    /// Показывает меню работы с наборами.
    /// </summary>
    private void ShowPresetMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Сохранить набор..."), false, SavePreset);
        menu.AddItem(new GUIContent("Загрузить набор..."), false, LoadPreset);
        menu.ShowAsContext();
    }

    /// <summary>
    /// Сохраняет текущий состав базы в файл.
    /// </summary>
    private void SavePreset()
    {
        serializedDatabase.ApplyModifiedProperties();

        string folder = PRSDKDatabasePresetService.DefaultFolder;
        // Имя окна в имени файла: наборы теперь частичные, и «database-preset» у пяти
        // окон превратился бы в пять одинаковых файлов с разным содержимым.
        string defaultName = $"{GetWindowKey()}-preset";

        string path = EditorUtility.SaveFilePanel(
            "Сохранить набор", folder, defaultName, "json");

        if (string.IsNullOrEmpty(path))
            return;

        PRSDKDatabasePreset preset = PRSDKDatabasePresetService.Capture(
            database, Path.GetFileNameWithoutExtension(path), IsCatalogInWindow);

        PRSDKDatabasePresetService.Save(preset, path);

        int items = preset.sections.Sum(section => section.items.Count);
        ShowNotification(new GUIContent($"Сохранено: {items} шт."));

        // Набор описывает только разделы этого окна: применять его будут там же,
        // а разделы, которых окно не показывает, оно и не вправе менять.
        Debug.Log(
            $"[PRSDKDatabase] Набор «{preset.name}» сохранён: {path}. " +
            $"Разделов: {preset.sections.Count} — из окна «{titleContent.text}».");
    }

    /// <summary>
    /// Короткое имя окна для имени файла набора.
    /// </summary>
    private string GetWindowKey()
    {
        if (string.IsNullOrEmpty(OwnedEditorMenuPath))
            return "database";

        string tail = OwnedEditorMenuPath.Substring(OwnedEditorMenuPath.LastIndexOf('/') + 1);
        return tail.Replace(' ', '-').ToLowerInvariant();
    }

    /// <summary>
    /// Читает набор и готовит отчёт; база пока не меняется.
    /// </summary>
    private void LoadPreset()
    {
        string path = EditorUtility.OpenFilePanel(
            "Загрузить набор базы", PRSDKDatabasePresetService.DefaultFolder, "json");

        if (string.IsNullOrEmpty(path))
            return;

        PRSDKDatabasePreset preset = PRSDKDatabasePresetService.Load(path, out string error);

        if (preset == null)
        {
            EditorUtility.DisplayDialog("Набор не прочитан", error, "Понятно");
            return;
        }

        pendingPreset = PRSDKDatabasePresetService.Analyze(preset, database, IsCatalogInWindow);
        presetReportScroll = Vector2.zero;
    }

    /// <summary>
    /// Каталог принадлежит этому окну.
    /// </summary>
    /// <remarks>
    /// Наборы работают в границах окна: сохраняется и применяется только то, что окно
    /// показывает. Иначе из каталога предметов можно молча переписать награды и звуки —
    /// увидеть это в отчёте нельзя, потому что таких разделов в окне попросту нет.
    /// <para>
    /// Путь каталога уходит вглубь («<c>&lt;Obby&gt;k__BackingField.&lt;Skins&gt;…</c>»),
    /// а решает корневой раздел базы: атрибут окна стоит на нём.
    /// </para>
    /// </remarks>
    private bool IsCatalogInWindow(SerializedProperty catalog)
    {
        if (catalog == null)
            return false;

        int dot = catalog.propertyPath.IndexOf('.');
        string root = dot < 0 ? catalog.propertyPath : catalog.propertyPath.Substring(0, dot);

        SerializedProperty property = serializedDatabase.FindProperty(root);

        if (property == null)
            return false;

        return IsSectionVisible(PRSDKInspectorUtility.GetFieldType(database.GetType(), property));
    }

    /// <summary>
    /// Рисует отчёт по разобранному набору.
    /// </summary>
    private void DrawPresetReport()
    {
        if (pendingPreset == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                $"Набор «{pendingPreset.Preset.name}»",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                $"Сохранён: {pendingPreset.Preset.savedAt}   •   проект: {pendingPreset.Preset.project}",
                EditorStyles.miniLabel);


            DrawPresetMode();

            string outgoing = presetApplyMode == PRSDKDatabasePresetApplyMode.Replace
                ? $"уйдёт из базы: {pendingPreset.OutgoingCount}"
                : $"останется сверх набора: {pendingPreset.OutgoingCount}";

            EditorGUILayout.LabelField(
                $"Встанет в базу: {pendingPreset.ResolvedCount}   •   {outgoing}   •   " +
                $"ошибок: {pendingPreset.ErrorCount}   •   предупреждений: {pendingPreset.WarningCount}");

            DrawPresetOutgoing();
            DrawPresetIssues();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(pendingPreset.IsEmpty))
                {
                    if (GUILayout.Button("Применить", GUILayout.Width(100f)))
                        ApplyPreset();
                }

                if (GUILayout.Button("Отмена", GUILayout.Width(80f)))
                    pendingPreset = null;

                GUILayout.FlexibleSpace();
            }

            if (pendingPreset != null && pendingPreset.IsEmpty)
                EditorGUILayout.HelpBox("Применять нечего: ни один каталог набора не подошёл.", MessageType.Error);
        }
    }

    /// <summary>
    /// Рисует выбор режима применения.
    /// </summary>
    private void DrawPresetMode()
    {
        var options = new[]
        {
            new GUIContent("Заменить состав", "Каталоги станут ровно такими, как в наборе. Лишнее уйдёт."),
            new GUIContent("Дополнить", "Существующее останется, из набора добавится недостающее.")
        };

        presetApplyMode = (PRSDKDatabasePresetApplyMode)GUILayout.Toolbar(
            (int)presetApplyMode, options, GUILayout.Height(20f));
    }

    /// <summary>
    /// Рассказывает о том, что лежит в базе сверх набора.
    /// </summary>
    /// <remarks>
    /// Не находка сверки, а следствие режима: при замене эти элементы уйдут,
    /// при дополнении останутся.
    /// </remarks>
    private void DrawPresetOutgoing()
    {
        if (pendingPreset.OutgoingCount == 0)
            return;

        IEnumerable<string> names = pendingPreset.Sections
            .Where(section => !section.Skipped)
            .SelectMany(section => section.Outgoing)
            .Where(asset => asset != null)
            .Select(asset => asset.name)
            .Take(6);

        string listed = string.Join(", ", names);
        if (pendingPreset.OutgoingCount > 6)
            listed += $" и ещё {pendingPreset.OutgoingCount - 6}";

        if (presetApplyMode == PRSDKDatabasePresetApplyMode.Replace)
        {
            EditorGUILayout.HelpBox(
                $"В базе есть {pendingPreset.OutgoingCount} элементов вне набора — будут убраны: {listed}.\n" +
                "Сами ассеты останутся в проекте.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"В базе есть {pendingPreset.OutgoingCount} элементов вне набора — останутся: {listed}.",
                MessageType.Info);
        }
    }

    /// <summary>
    /// Рисует находки сверки, самые важные сверху.
    /// </summary>
    private void DrawPresetIssues()
    {
        if (pendingPreset.Issues.Count == 0)
        {
            EditorGUILayout.HelpBox("Набор сошёлся с проектом полностью.", MessageType.Info);
            return;
        }

        using (var scroll = new EditorGUILayout.ScrollViewScope(presetReportScroll, GUILayout.MaxHeight(160f)))
        {
            IEnumerable<PRSDKDatabasePresetIssue> ordered = pendingPreset.Issues
                .OrderByDescending(issue => issue.Severity);

            foreach (PRSDKDatabasePresetIssue issue in ordered)
            {
                EditorGUILayout.HelpBox(
                    $"{issue.Section}: {issue.Message}",
                    ToMessageType(issue.Severity));
            }

            presetReportScroll = scroll.scrollPosition;
        }
    }

    private static MessageType ToMessageType(PRSDKDatabasePresetSeverity severity)
    {
        return severity switch
        {
            PRSDKDatabasePresetSeverity.Error => MessageType.Error,
            PRSDKDatabasePresetSeverity.Warning => MessageType.Warning,
            _ => MessageType.Info
        };
    }

    /// <summary>
    /// Записывает разобранный набор в базу.
    /// </summary>
    private void ApplyPreset()
    {
        bool replace = presetApplyMode == PRSDKDatabasePresetApplyMode.Replace;

        var lines = new List<string>
        {
            replace
                ? "Состав каталогов будет заменён набором."
                : "Набор добавится к текущему составу каталогов."
        };

        if (replace && pendingPreset.OutgoingCount > 0)
        {
            lines.Add(
                $"Из базы уйдёт элементов: {pendingPreset.OutgoingCount}. " +
                "Сами ассеты останутся в проекте.");
        }

        if (pendingPreset.ErrorCount > 0)
            lines.Add($"Записей набора не удалось применить: {pendingPreset.ErrorCount}.");

        lines.Add("Продолжить?");

        string message = string.Join("\n\n", lines);

        if (!EditorUtility.DisplayDialog("Применить набор", message, "Применить", "Отмена"))
            return;

        int changed = PRSDKDatabasePresetService.Apply(pendingPreset, database, presetApplyMode);

        pendingPreset = null;
        assetCache.Clear();
        serializedDatabase = new SerializedObject(database);

        ShowNotification(new GUIContent($"Каталогов обновлено: {changed}"));
        Repaint();
    }

    #endregion

    /// <summary>
    /// Рисует разделы, которыми занимаются другие окна.
    /// </summary>
    /// <remarks>
    /// Содержимое не показывается: те же данные в двух редакторах — верный способ
    /// получить разъехавшиеся правки. Разделы одного окна собираются в строку с общей
    /// кнопкой: пять одинаковых кнопок подряд не сообщают больше, чем одна.
    /// </remarks>
    private void DrawExternalLinks(
        IReadOnlyList<(DatabaseExternalEditorAttribute Editor, string Section)> links)
    {
        if (links.Count == 0)
            return;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Правится в других окнах", EditorStyles.miniBoldLabel);

        IEnumerable<IGrouping<string, (DatabaseExternalEditorAttribute Editor, string Section)>> groups =
            links.GroupBy(link => link.Editor.MenuPath);

        foreach (var group in groups)
        {
            DatabaseExternalEditorAttribute editor = group.First().Editor;
            string sections = string.Join(", ", group.Select(link => link.Section));

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                string window = string.IsNullOrEmpty(editor.WindowName)
                    ? "Открыть"
                    : editor.WindowName;

                EditorGUILayout.LabelField(
                    new GUIContent($"{window}: {sections}", editor.Description),
                    EditorStyles.label);

                if (GUILayout.Button("Открыть", GUILayout.Width(90f)))
                    EditorApplication.ExecuteMenuItem(editor.MenuPath);
            }
        }
    }

    /// <summary>
    /// Пункт меню окна, которому принадлежат помеченные секции.
    /// </summary>
    /// <remarks>
    /// Пусто — это общее окно базы: разделы со своим редактором оно показывает строкой
    /// со ссылкой. У специализированного окна здесь его собственный путь меню, и «свои»
    /// разделы оно рисует полностью, а чужие не показывает вовсе.
    /// </remarks>
    protected virtual string OwnedEditorMenuPath => null;

    /// <summary>
    /// Показывать ли секцию в этом окне.
    /// </summary>
    private bool IsSectionVisible(Type fieldType)
    {
        DatabaseExternalEditorAttribute external = GetExternalEditor(fieldType);

        // Общее окно показывает всё: чужие разделы — строкой со ссылкой.
        if (string.IsNullOrEmpty(OwnedEditorMenuPath))
            return true;

        return external != null && external.MenuPath == OwnedEditorMenuPath;
    }

    /// <summary>
    /// Атрибут внешнего редактора у типа секции.
    /// </summary>
    private static DatabaseExternalEditorAttribute GetExternalEditor(Type fieldType)
    {
        return fieldType == null
            ? null
            : Attribute.GetCustomAttribute(
                fieldType,
                typeof(DatabaseExternalEditorAttribute),
                inherit: true) as DatabaseExternalEditorAttribute;
    }

    private void DrawSection(SerializedProperty property, string sectionName, bool alwaysExpanded = false)
    {
        Type fieldType = PRSDKInspectorUtility.GetFieldType(database.GetType(), property);
        object sectionValue = PRSDKInspectorUtility.GetFieldValue(database, property);
        DrawSection(property, sectionName, fieldType, sectionValue, alwaysExpanded);
    }

    private void DrawSection(
        SerializedProperty property,
        string sectionName,
        Type fieldType,
        object sectionValue,
        bool alwaysExpanded = false)
    {
        Type elementType = PRSDKInspectorUtility.GetDatabaseElementType(fieldType);
        var options = fieldType != null
            ? Attribute.GetCustomAttribute(
                fieldType,
                typeof(DatabaseEditorOptionsAttribute),
                inherit: true) as DatabaseEditorOptionsAttribute
            : null;
        options ??= new DatabaseEditorOptionsAttribute();
        SerializedProperty data = elementType != null ? property.FindPropertyRelative("data") : null;
        bool supportsAssetTools =
            elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);
        bool useGrid = SupportsGrid(elementType, supportsAssetTools, options);
        IReadOnlyList<SerializedProperty> childProperties =
            PRSDKInspectorUtility.GetDirectChildren(property);
        bool hasNestedDatabases = elementType == null && childProperties.Any(child =>
            PRSDKInspectorUtility.GetDatabaseElementType(
                PRSDKInspectorUtility.GetFieldType(fieldType, child)) != null);
        string count = data is { isArray: true } ? $"  ({data.arraySize})" : string.Empty;

        // Единственный раздел-контейнер свой заголовок не рисует: в окне наград «Rewards»
        // ничего не различает — внутри и так одни награды, а рамка с подписью только
        // отнимает место у списков.
        if (alwaysExpanded && hasNestedDatabases)
        {
            DrawNestedSections(childProperties, fieldType, sectionValue);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (alwaysExpanded && (useGrid || hasNestedDatabases))
            {
                property.isExpanded = true;
                EditorGUILayout.LabelField(sectionName + count, EditorStyles.boldLabel);
            }
            else if (useGrid || hasNestedDatabases)
            {
                property.isExpanded = EditorGUILayout.Foldout(
                    property.isExpanded,
                    new GUIContent(sectionName + count),
                    true,
                    EditorStyles.foldoutHeader);
            }
            else
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(sectionName + count),
                    includeChildren: true);
            }

            if (!property.isExpanded)
                return;

            if (hasNestedDatabases)
            {
                DrawNestedSections(childProperties, fieldType, sectionValue);
                return;
            }

            if (data is { isArray: true } && supportsAssetTools)
                DrawDatabaseTools(data, elementType, options);

            DatabaseValidationIssue[] validationIssues = Array.Empty<DatabaseValidationIssue>();
            Exception validationException = null;
            if (sectionValue is IDatabaseValidationProvider validationProvider &&
                (options.ShowValidation || useGrid))
            {
                validationIssues = GetValidationIssues(validationProvider, out validationException);
            }

            if (options.ShowValidation && sectionValue is IDatabaseValidationProvider)
            {
                int availableAssetCount = supportsAssetTools
                    ? GetAvailableAssets(elementType).Length
                    : -1;
                int itemCount = data is { isArray: true } ? data.arraySize : -1;
                DrawValidation(
                    validationIssues,
                    validationException,
                    sectionName,
                    itemCount,
                    availableAssetCount);
            }

            if (useGrid && data is { isArray: true })
            {
                DrawAssetGrid(
                    property.propertyPath,
                    data,
                    elementType,
                    validationIssues);
            }
        }

        EditorGUILayout.Space(2f);
    }

    private void DrawNestedSections(
        IReadOnlyList<SerializedProperty> childProperties,
        Type parentType,
        object parentValue)
    {
        foreach (SerializedProperty child in childProperties)
        {
            Type childType = PRSDKInspectorUtility.GetFieldType(parentType, child);
            Type childElementType = PRSDKInspectorUtility.GetDatabaseElementType(childType);
            if (childElementType == null)
            {
                EditorGUILayout.PropertyField(child, includeChildren: true);
                continue;
            }

            object childValue = PRSDKInspectorUtility.GetFieldValue(parentValue, child);
            DrawSection(
                child,
                PRSDKInspectorUtility.GetSectionName(child),
                childType,
                childValue);
        }
    }

    private static bool SupportsGrid(
        Type elementType,
        bool supportsAssetTools,
        DatabaseEditorOptionsAttribute options)
    {
        // Сетка сама по себе не про предметы, а про то, что показывают картинкой:
        // награду от награды отличают по иконке так же, как шапку от шапки. Признаком
        // служит IIconProvider — заводить ради этого настройку в каждом каталоге значит
        // повторять одно и то же решение.
        return supportsAssetTools &&
               (options.Presentation == DatabaseEditorPresentation.Grid ||
                options.Presentation == DatabaseEditorPresentation.Auto &&
                (typeof(ItemDefinitionBase).IsAssignableFrom(elementType) ||
                 typeof(IIconProvider).IsAssignableFrom(elementType)));
    }

    private void DrawDatabaseTools(
        SerializedProperty data,
        Type elementType,
        DatabaseEditorOptionsAttribute options)
    {
        UnityEngine.Object[] availableAssets = GetAvailableAssets(elementType);
        int missingCount = CountMissingAssets(data, availableAssets);

        if (!options.ShowAddAll && !options.ShowRemoveNull && !options.ShowClear)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (options.ShowAddAll)
            {
                using (new EditorGUI.DisabledScope(missingCount == 0))
                {
                    if (GUILayout.Button($"Добавить все ({missingCount})"))
                        AddAll(data, availableAssets, elementType);
                }
            }

            if (options.ShowRemoveNull)
            {
                using (new EditorGUI.DisabledScope(!HasNullReferences(data)))
                {
                    if (GUILayout.Button("Убрать null"))
                        RemoveNullReferences(data);
                }
            }

            if (options.ShowClear)
            {
                using (new EditorGUI.DisabledScope(data.arraySize == 0))
                {
                    if (GUILayout.Button("Очистить"))
                        Clear(data, elementType);
                }
            }
        }
    }

    private void DrawAssetGrid(
        string sectionKey,
        SerializedProperty data,
        Type elementType,
        IReadOnlyCollection<DatabaseValidationIssue> validationIssues)
    {
        PruneSelection(sectionKey, data);
        HandleGridShortcuts(sectionKey, data);

        UnityEngine.Object selected = ResolveSelectedAsset(sectionKey, data);
        AssetGridViewState viewState = GetGridViewState(sectionKey);
        AssetCardEntry[] visibleEntries = GetVisibleEntries(data, viewState, validationIssues);
        float panelHeight = Mathf.Max(380f, position.height - 210f);
        float availableWidth = Mathf.Max(580f, position.width - 24f);
        float leftWidth = Mathf.Clamp(
            availableWidth * gridSplit,
            280f,
            Mathf.Max(280f, availableWidth - 286f));

        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(panelHeight)))
        {
            using (new EditorGUILayout.VerticalScope(
                       GUILayout.Width(leftWidth),
                       GUILayout.Height(panelHeight)))
            {
                DrawSingleAssetField(sectionKey, data, elementType);
                DrawAssetGridToolbar(viewState, visibleEntries.Length, data.arraySize, CollectCategories(data));

                Vector2 gridScroll = GetScrollPosition(gridScrollPositions, sectionKey);
                using (var gridScrollView = new EditorGUILayout.ScrollViewScope(
                           gridScroll,
                           GUILayout.ExpandHeight(true)))
                {
                    DrawAssetCards(sectionKey, visibleEntries, leftWidth);
                    gridScroll = gridScrollView.scrollPosition;
                }

                gridScrollPositions[sectionKey] = gridScroll;
            }

            Rect splitterRect = GUILayoutUtility.GetRect(
                6f,
                6f,
                GUILayout.Width(6f),
                GUILayout.Height(panelHeight));
            DrawGridSplitter(splitterRect, availableWidth);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox,
                       GUILayout.ExpandWidth(true),
                       GUILayout.Height(panelHeight)))
            {
                DrawSelectedAsset(sectionKey, data, selected);
            }
        }
    }

    private AssetGridViewState GetGridViewState(string sectionKey)
    {
        if (gridViewStates.TryGetValue(sectionKey, out AssetGridViewState state))
            return state;

        state = new AssetGridViewState();
        gridViewStates[sectionKey] = state;
        return state;
    }

    private static void DrawAssetGridToolbar(
        AssetGridViewState state,
        int visibleCount,
        int totalCount,
        string[] categories)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Сортировка", GUILayout.Width(76f));
                state.SortMode = (AssetSortMode)EditorGUILayout.Popup(
                    (int)state.SortMode,
                    SortLabels);

                string direction = state.SortDescending ? "↓" : "↑";
                string tooltip = state.SortDescending
                    ? "По убыванию"
                    : "По возрастанию";
                if (GUILayout.Button(new GUIContent(direction, tooltip), GUILayout.Width(28f)))
                    state.SortDescending = !state.SortDescending;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Качество", GUILayout.Width(76f));
                state.QualityFilter = DrawQualityFilter(state.QualityFilter);
            }

            DrawCategoryFilter(state, categories);

            using (new EditorGUILayout.HorizontalScope())
            {
                state.InvalidOnly = GUILayout.Toggle(
                    state.InvalidOnly,
                    new GUIContent("Только с ошибками", "Показывать элементы с проблемами валидации."),
                    GUILayout.Width(142f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"{visibleCount}/{totalCount}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(48f));
            }
        }
    }

    /// <summary>
    /// Вид элемента каталога.
    /// </summary>
    /// <remarks>
    /// Пусто у элементов, которые о видах не заявляют: каталог однородных предметов
    /// фильтровать незачем.
    /// </remarks>
    private static string GetAssetCategory(UnityEngine.Object asset)
    {
        return asset is IDatabaseCategoryProvider provider ? provider.DatabaseCategory ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Виды, встречающиеся в каталоге.
    /// </summary>
    private static string[] CollectCategories(SerializedProperty data)
    {
        var categories = new SortedSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < data.arraySize; index++)
        {
            string category = GetAssetCategory(data.GetArrayElementAtIndex(index).objectReferenceValue);

            if (!string.IsNullOrEmpty(category))
                categories.Add(category);
        }

        return categories.ToArray();
    }

    /// <summary>
    /// Рисует выбор вида.
    /// </summary>
    /// <remarks>
    /// Появляется только там, где видов больше одного: в каталоге шапок фильтр
    /// по «шапке» ничего не даёт.
    /// </remarks>
    private static void DrawCategoryFilter(AssetGridViewState state, string[] categories)
    {
        if (categories.Length < 2)
        {
            state.CategoryFilter = string.Empty;
            return;
        }

        var labels = new string[categories.Length + 1];
        labels[0] = "Все";

        for (int index = 0; index < categories.Length; index++)
            labels[index + 1] = Nicify(categories[index]);

        int selected = System.Array.IndexOf(categories, state.CategoryFilter) + 1;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Вид", GUILayout.Width(76f));
            selected = EditorGUILayout.Popup(Mathf.Clamp(selected, 0, categories.Length), labels);
        }

        state.CategoryFilter = selected <= 0 ? string.Empty : categories[selected - 1];
    }

    /// <summary>
    /// Убирает у имени типа служебный суффикс.
    /// </summary>
    /// <remarks>
    /// В списке читается «Hat», а не «HatDefinition»: слово повторяется у каждого вида
    /// и ничего не различает.
    /// </remarks>
    private static string Nicify(string category)
    {
        const string suffix = "Definition";

        return category.EndsWith(suffix, StringComparison.Ordinal) && category.Length > suffix.Length
            ? category.Substring(0, category.Length - suffix.Length)
            : category;
    }

    private static int DrawQualityFilter(int qualityFilter)
    {
        string[] qualityNames = Enum.GetNames(typeof(QualityType));
        string[] labels = new string[qualityNames.Length + 1];
        labels[0] = "Все";
        Array.Copy(qualityNames, 0, labels, 1, qualityNames.Length);
        int selectedIndex = Mathf.Clamp(qualityFilter + 1, 0, labels.Length - 1);
        selectedIndex = EditorGUILayout.Popup(selectedIndex, labels);
        return selectedIndex - 1;
    }

    private static AssetCardEntry[] GetVisibleEntries(
        SerializedProperty data,
        AssetGridViewState state,
        IReadOnlyCollection<DatabaseValidationIssue> validationIssues)
    {
        var invalidIndices = new HashSet<int>(validationIssues
            .Where(issue => issue != null && issue.Index >= 0)
            .Select(issue => issue.Index));
        var entries = new List<AssetCardEntry>(data.arraySize);

        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object asset = data.GetArrayElementAtIndex(index).objectReferenceValue;
            bool isInvalid = invalidIndices.Contains(index);
            if (state.InvalidOnly && !isInvalid)
                continue;

            if (state.QualityFilter >= 0 &&
                (asset is not IQualityProvider qualityProvider ||
                 (int)qualityProvider.Quality != state.QualityFilter))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(state.CategoryFilter) &&
                GetAssetCategory(asset) != state.CategoryFilter)
            {
                continue;
            }

            entries.Add(new AssetCardEntry(asset, index, isInvalid));
        }

        entries.Sort((left, right) => CompareEntries(left, right, state));
        return entries.ToArray();
    }

    private static int CompareEntries(
        AssetCardEntry left,
        AssetCardEntry right,
        AssetGridViewState state)
    {
        int comparison = state.SortMode switch
        {
            AssetSortMode.Quality => GetQualityOrder(left.Asset).CompareTo(GetQualityOrder(right.Asset)),
            AssetSortMode.Name => string.Compare(
                GetAssetName(left.Asset),
                GetAssetName(right.Asset),
                StringComparison.OrdinalIgnoreCase),
            _ => left.DatabaseIndex.CompareTo(right.DatabaseIndex)
        };

        if (state.SortDescending)
            comparison = -comparison;

        return comparison != 0
            ? comparison
            : left.DatabaseIndex.CompareTo(right.DatabaseIndex);
    }

    private static int GetQualityOrder(UnityEngine.Object asset)
    {
        return asset is IQualityProvider qualityProvider
            ? (int)qualityProvider.Quality
            : -1;
    }

    private void DrawGridSplitter(Rect rect, float availableWidth)
    {
        EditorGUI.DrawRect(rect, new Color(0.20f, 0.20f, 0.20f, 1f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
        {
            resizeGridSplit = true;
            current.Use();
        }

        if (resizeGridSplit && current.type == EventType.MouseDrag)
        {
            gridSplit = Mathf.Clamp((current.mousePosition.x - 8f) / availableWidth, 0.35f, 0.75f);
            Repaint();
            current.Use();
        }

        if (resizeGridSplit && current.rawType == EventType.MouseUp)
        {
            resizeGridSplit = false;
            current.Use();
        }
    }

    private void DrawSingleAssetField(
        string sectionKey,
        SerializedProperty data,
        Type elementType)
    {
        UnityEngine.Object asset = EditorGUILayout.ObjectField(
            "Добавить asset",
            null,
            elementType,
            allowSceneObjects: false);
        if (asset != null)
            AddSingleAsset(sectionKey, data, asset, elementType);
    }

    private void DrawAssetCards(
        string sectionKey,
        IReadOnlyList<AssetCardEntry> entries,
        float availableWidth)
    {
        EnsureCardStyles();
        int columnCount = Mathf.Max(1, Mathf.FloorToInt((availableWidth - 22f) / (CardWidth + 8f)));
        int index = 0;

        while (index < entries.Count)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int column = 0; column < columnCount && index < entries.Count; column++, index++)
                {
                    AssetCardEntry entry = entries[index];
                    DrawAssetCard(
                        sectionKey,
                        entries,
                        index,
                        IsSelected(sectionKey, entry.Asset),
                        entry.IsInvalid);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(5f);
        }

        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Нет элементов, соответствующих текущим фильтрам.",
                MessageType.Info);
        }
    }

    private void DrawAssetCard(
        string sectionKey,
        IReadOnlyList<AssetCardEntry> entries,
        int entryIndex,
        bool selected,
        bool isInvalid)
    {
        UnityEngine.Object asset = entries[entryIndex].Asset;

        Rect cardRect = GUILayoutUtility.GetRect(
            CardWidth,
            CardHeight,
            GUILayout.Width(CardWidth),
            GUILayout.Height(CardHeight));

        Color accent = asset is IQualityProvider qualityProvider
            ? QualityUtils.GetColor(qualityProvider.Quality)
            : new Color(0.35f, 0.48f, 0.70f, 1f);
        Color background = Color.Lerp(new Color(0.13f, 0.14f, 0.17f, 1f), accent, 0.18f);
        EditorGUI.DrawRect(cardRect, background);
        DrawBorder(cardRect, selected ? accent : Color.Lerp(background, Color.white, 0.14f), selected ? 3f : 1f);

        string tooltip = asset != null ? AssetDatabase.GetAssetPath(asset) : "Пустая ссылка";
        if (isInvalid)
            tooltip += "\nЕсть проблемы валидации";
        tooltip += "\nCtrl — добавить к выделению, Shift — диапазон, Del — убрать из базы";

        if (GUI.Button(cardRect, new GUIContent(string.Empty, tooltip), GUIStyle.none))
            HandleCardClick(sectionKey, entries, entryIndex);

        Rect iconRect = new(cardRect.x + 8f, cardRect.y + 8f, cardRect.width - 16f, 88f);
        Sprite icon = asset is IIconProvider iconProvider ? iconProvider.Icon : null;
        Color? tint = ResolveIconTint(asset);

        if (icon != null)
            DrawSprite(iconRect, icon, OpaqueTint(tint));
        else if (tint.HasValue)
            DrawColorSwatch(iconRect, tint.Value);
        else
            GUI.Label(iconRect, asset == null ? "NULL" : "Нет иконки", EditorStyles.centeredGreyMiniLabel);

        Rect labelRect = new(cardRect.x + 6f, iconRect.yMax + 5f, cardRect.width - 12f, 30f);
        GUI.Label(labelRect, GetAssetName(asset), cardNameStyle);

        DrawCardBadges(cardRect, asset);

        if (isInvalid)
        {
            Rect badgeRect = new(cardRect.xMax - 24f, cardRect.y + 4f, 20f, 20f);
            EditorGUI.DrawRect(badgeRect, new Color(0.78f, 0.16f, 0.16f, 1f));
            GUI.Label(
                badgeRect,
                new GUIContent("!", "Есть проблемы валидации"),
                invalidBadgeStyle);
        }
    }

    /// <summary>
    /// Находит блоки модулей.
    /// </summary>
    private IDatabaseAssetInspector[] GetAssetInspectors()
    {
        if (assetInspectors != null)
            return assetInspectors;

        assetInspectors = TypeCache.GetTypesDerivedFrom<IDatabaseAssetInspector>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
            .Select(type =>
            {
                try
                {
                    return (IDatabaseAssetInspector)Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[PRSDKDatabase] Блок «{type.Name}» не создан: {exception.Message}");
                    return null;
                }
            })
            .Where(inspector => inspector != null)
            .OrderBy(inspector => inspector.Order)
            .ToArray();

        return assetInspectors;
    }

    /// <summary>
    /// Находит метки модулей.
    /// </summary>
    private IDatabaseCardBadge[] GetCardBadges()
    {
        if (cardBadges != null)
            return cardBadges;

        cardBadges = TypeCache.GetTypesDerivedFrom<IDatabaseCardBadge>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
            .Select(type =>
            {
                try
                {
                    return (IDatabaseCardBadge)Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[PRSDKDatabase] Метка «{type.Name}» не создана: {exception.Message}");
                    return null;
                }
            })
            .Where(badge => badge != null)
            .OrderBy(badge => badge.Order)
            .ToArray();

        return cardBadges;
    }

    /// <summary>
    /// Рисует метки модулей в нижней полосе карточки.
    /// </summary>
    /// <remarks>
    /// Метки идут слева направо и обрезаются по ширине карточки: лучше показать первую
    /// целиком, чем две половинками.
    /// </remarks>
    private void DrawCardBadges(Rect cardRect, UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        const float height = 18f;
        const float padding = 5f;

        float x = cardRect.x + padding;
        float limit = cardRect.xMax - padding;
        float y = cardRect.yMax - height - 3f;

        foreach (IDatabaseCardBadge badge in GetCardBadges())
        {
            try
            {
                if (!badge.CanDraw(asset))
                    continue;

                float width = Mathf.Max(0f, badge.GetWidth(asset));

                if (width <= 0f || x + width > limit)
                    continue;

                badge.Draw(new Rect(x, y, width, height), asset);
                x += width + 3f;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PRSDKDatabase] Метка «{badge.GetType().Name}» не нарисована: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Рисует блоки модулей под инспектором ассета.
    /// </summary>
    /// <remarks>
    /// Каждый блок изолирован: исключение в надстройке не должно ронять отрисовку окна
    /// целиком.
    /// </remarks>
    private void DrawAssetInspectorExtensions(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        foreach (IDatabaseAssetInspector inspector in GetAssetInspectors())
        {
            bool draws;

            try
            {
                draws = inspector.CanDraw(asset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PRSDKDatabase] Блок «{inspector.GetType().Name}» опрошен с ошибкой: {exception.Message}");
                continue;
            }

            if (!draws)
                continue;

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                try
                {
                    inspector.Draw(asset);
                }
                catch (Exception exception)
                {
                    EditorGUILayout.HelpBox(
                        $"Блок «{inspector.GetType().Name}» не нарисован: {exception.Message}",
                        MessageType.Error);
                }
            }
        }
    }

    #region Выделение

    /// <summary>
    /// Выделенные карточки секции.
    /// </summary>
    private HashSet<UnityEngine.Object> GetSelection(string sectionKey)
    {
        if (selections.TryGetValue(sectionKey, out HashSet<UnityEngine.Object> selection))
            return selection;

        selection = new HashSet<UnityEngine.Object>();
        selections[sectionKey] = selection;
        return selection;
    }

    private bool IsSelected(string sectionKey, UnityEngine.Object asset)
    {
        return asset != null
               && selections.TryGetValue(sectionKey, out HashSet<UnityEngine.Object> selection)
               && selection.Contains(asset);
    }

    /// <summary>
    /// Обрабатывает щелчок по карточке с учётом модификаторов.
    /// </summary>
    /// <remarks>
    /// Правила те же, что в Project и Hierarchy: обычный щелчок выделяет одну карточку,
    /// Ctrl добавляет и убирает по одной, Shift берёт диапазон от последней отмеченной.
    /// </remarks>
    private void HandleCardClick(string sectionKey, IReadOnlyList<AssetCardEntry> entries, int entryIndex)
    {
        activeSectionKey = sectionKey;

        UnityEngine.Object asset = entries[entryIndex].Asset;
        HashSet<UnityEngine.Object> selection = GetSelection(sectionKey);
        Event current = Event.current;
        bool additive = current.control || current.command;

        if (current.shift && selectionAnchors.TryGetValue(sectionKey, out int anchor)
                          && anchor >= 0 && anchor < entries.Count)
        {
            if (!additive)
                selection.Clear();

            int from = Mathf.Min(anchor, entryIndex);
            int to = Mathf.Max(anchor, entryIndex);

            for (int index = from; index <= to; index++)
            {
                if (entries[index].Asset != null)
                    selection.Add(entries[index].Asset);
            }
        }
        else if (additive)
        {
            if (asset != null && !selection.Add(asset))
                selection.Remove(asset);

            selectionAnchors[sectionKey] = entryIndex;
        }
        else
        {
            selection.Clear();

            if (asset != null)
                selection.Add(asset);

            selectionAnchors[sectionKey] = entryIndex;
        }

        // Активной остаётся та, по которой щёлкнули: её свойства и показывает панель справа.
        selectedAssets[sectionKey] = asset;
        Repaint();
    }

    /// <summary>
    /// Спрашивает, точно ли убирать выбранное из каталога.
    /// </summary>
    /// <remarks>
    /// Про одну карточку спрашивает по имени: так видно, что именно уйдёт, если клавишу
    /// нажали не глядя.
    /// </remarks>
    private static bool ConfirmRemoval(ICollection<UnityEngine.Object> targets)
    {
        string message = targets.Count == 1
            ? $"Убрать «{GetAssetName(targets.First())}» из каталога?"
            : $"Убрать из каталога элементов: {targets.Count}?";

        return EditorUtility.DisplayDialog(
            "Удалить из базы",
            $"{message}\n\nСам ассет останется в проекте.",
            "Удалить",
            "Отмена");
    }

    /// <summary>
    /// Убирает из выделения то, чего в каталоге уже нет.
    /// </summary>
    /// <remarks>
    /// Каталог меняется и мимо окна — набором, кнопкой «Убрать null», правкой ассета.
    /// Выделение, пережившее такое изменение, привело бы к удалению не того.
    /// </remarks>
    private void PruneSelection(string sectionKey, SerializedProperty data)
    {
        if (!selections.TryGetValue(sectionKey, out HashSet<UnityEngine.Object> selection)
            || selection.Count == 0)
        {
            return;
        }

        var present = new HashSet<UnityEngine.Object>();

        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object value = data.GetArrayElementAtIndex(index).objectReferenceValue;

            if (value != null)
                present.Add(value);
        }

        selection.RemoveWhere(asset => asset == null || !present.Contains(asset));
    }

    /// <summary>
    /// Обрабатывает клавиши в сетке каталога.
    /// </summary>
    /// <remarks>
    /// Только Delete. Backspace здесь не годится: окно ловит его раньше текстовых полей,
    /// и правку значения в карточке пришлось бы вести без стирания символов.
    /// <para>
    /// Пока правят поле свойств, клавиша принадлежит полю: игрок стирает значение
    /// Scale Factor, а не предмет из каталога.
    /// </para>
    /// </remarks>
    private void HandleGridShortcuts(string sectionKey, SerializedProperty data)
    {
        Event current = Event.current;

        if (current.type != EventType.KeyDown || activeSectionKey != sectionKey)
            return;

        if (current.keyCode != KeyCode.Delete)
            return;

        if (EditorGUIUtility.editingTextField)
            return;

        UnityEngine.Object[] selected = GetSelection(sectionKey)
            .Where(asset => asset != null)
            .ToArray();

        if (selected.Length == 0)
            return;

        current.Use();

        // Клавишу можно задеть случайно, в отличие от кнопки, поэтому спрашиваем всегда -
        // даже об одной карточке.
        RemoveAssets(sectionKey, data, selected, alwaysConfirm: true);
    }

    #endregion

    private void DrawSelectedAsset(
        string sectionKey,
        SerializedProperty data,
        UnityEngine.Object selected)
    {
        EditorGUILayout.LabelField("Свойства", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField(selected, typeof(UnityEngine.Object), false);

        if (selected == null)
        {
            EditorGUILayout.HelpBox("Выберите карточку слева.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(GetAssetName(selected), EditorStyles.largeLabel);
        if (selected is IQualityProvider qualityProvider)
        {
            Color previousColor = GUI.color;
            GUI.color = QualityUtils.GetColor(qualityProvider.Quality);
            EditorGUILayout.LabelField(qualityProvider.Quality.ToString(), EditorStyles.boldLabel);
            GUI.color = previousColor;
        }

        UnityEngine.Object[] selection = GetSelection(sectionKey)
            .Where(asset => asset != null)
            .ToArray();

        // Кнопка работает с выделением целиком, но одиночный выбор в него попадает
        // не всегда - например, когда карточку подставил ResolveSelectedAsset.
        UnityEngine.Object[] targets = selection.Length > 0 ? selection : new[] { selected };

        if (selection.Length > 1)
        {
            EditorGUILayout.LabelField(
                $"Выделено карточек: {selection.Length}",
                EditorStyles.miniBoldLabel);
        }

        string label = targets.Length > 1
            ? $"Удалить из базы ({targets.Length})"
            : "Удалить из базы";

        if (GUILayout.Button(label))
            RemoveAssets(sectionKey, data, targets);

        EditorGUILayout.Space(4f);
        Vector2 detailsScroll = GetScrollPosition(detailsScrollPositions, sectionKey);
        using (var detailsScrollView = new EditorGUILayout.ScrollViewScope(
                   detailsScroll,
                   GUILayout.ExpandHeight(true)))
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(
                position.width * (1f - gridSplit) * 0.38f,
                110f,
                220f);
            try
            {
                GetOrCreateAssetEditor(sectionKey, selected)?.OnInspectorGUI();
                DrawAssetInspectorExtensions(selected);
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }

            detailsScroll = detailsScrollView.scrollPosition;
        }

        detailsScrollPositions[sectionKey] = detailsScroll;
    }

    private void AddSingleAsset(
        string sectionKey,
        SerializedProperty data,
        UnityEngine.Object asset,
        Type elementType)
    {
        if (asset == null || !elementType.IsInstanceOfType(asset))
            return;

        for (int index = 0; index < data.arraySize; index++)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue == asset)
            {
                selectedAssets[sectionKey] = asset;
                return;
            }
        }

        Undo.RecordObject(database, $"Add {elementType.Name} asset");
        int newIndex = data.arraySize;
        data.InsertArrayElementAtIndex(newIndex);
        data.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        selectedAssets[sectionKey] = asset;
        GUIUtility.ExitGUI();
    }

    /// <summary>
    /// Убирает выбранные элементы из каталога.
    /// </summary>
    /// <remarks>
    /// Обход с конца: удаление сдвигает индексы, и при проходе вперёд часть элементов
    /// проскочила бы мимо.
    /// </remarks>
    /// <param name="alwaysConfirm">
    /// Спрашивать подтверждение даже об одной карточке. Нужно при удалении клавишей:
    /// её можно задеть случайно, в отличие от кнопки, до которой надо дотянуться.
    /// </param>
    private void RemoveAssets(
        string sectionKey,
        SerializedProperty data,
        IReadOnlyCollection<UnityEngine.Object> assets,
        bool alwaysConfirm = false)
    {
        var targets = new HashSet<UnityEngine.Object>(assets.Where(asset => asset != null));

        if (targets.Count == 0)
            return;

        if ((alwaysConfirm || targets.Count > 1) && !ConfirmRemoval(targets))
            return;

        Undo.RecordObject(database, "Remove database assets");

        bool removed = false;

        for (int index = data.arraySize - 1; index >= 0; index--)
        {
            UnityEngine.Object value = data.GetArrayElementAtIndex(index).objectReferenceValue;

            if (value == null || !targets.Contains(value))
                continue;

            DeleteArrayElement(data, index);
            removed = true;
        }

        if (!removed)
            return;

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);

        GetSelection(sectionKey).Clear();
        selectionAnchors.Remove(sectionKey);
        selectedAssets.Remove(sectionKey);
        DestroySelectedAssetEditor(sectionKey);
        GUIUtility.ExitGUI();
    }

    private UnityEngine.Object ResolveSelectedAsset(string sectionKey, SerializedProperty data)
    {
        if (selectedAssets.TryGetValue(sectionKey, out UnityEngine.Object selected))
        {
            for (int index = 0; index < data.arraySize; index++)
            {
                if (data.GetArrayElementAtIndex(index).objectReferenceValue == selected)
                    return selected;
            }
        }

        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object asset = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (asset == null)
                continue;

            selectedAssets[sectionKey] = asset;
            return asset;
        }

        selectedAssets.Remove(sectionKey);
        return null;
    }

    private static Vector2 GetScrollPosition(
        IReadOnlyDictionary<string, Vector2> positions,
        string sectionKey)
    {
        return positions.TryGetValue(sectionKey, out Vector2 position) ? position : Vector2.zero;
    }

    private void EnsureCardStyles()
    {
        if (cardNameStyle == null)
        {
            cardNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            // Цвет задаётся явно: подложка карточки всегда тёмная, а редакторский
            // miniLabel в светлой теме почти чёрный — на ней имя не прочитать.
            cardNameStyle.normal.textColor = new Color(0.93f, 0.95f, 0.99f, 1f);
        }
        if (invalidBadgeStyle == null)
        {
            invalidBadgeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            invalidBadgeStyle.normal.textColor = Color.white;
        }
    }

    private static string GetAssetName(UnityEngine.Object asset)
    {
        if (asset is INameProvider nameProvider && !string.IsNullOrWhiteSpace(nameProvider.Name))
            return nameProvider.Name;

        return asset != null ? asset.name : "NULL";
    }

    /// <summary>
    /// Возвращает цвет, в который нужно покрасить иконку, либо <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Цвета тела и варианты одного эффекта делят иконку между собой, и без покраски
    /// в сетке их не различить.
    /// </remarks>
    private static Color? ResolveIconTint(UnityEngine.Object asset)
    {
        return asset is IIconTintProvider provider && provider.TintIcon
            ? provider.IconTint
            : null;
    }

    /// <summary>
    /// Убирает прозрачность из цвета покраски.
    /// </summary>
    /// <remarks>
    /// Прозрачность у предмета означает вид в игре, а не в списке: цвет с нулевой альфой
    /// сделал бы карточку пустой, и предмет пропал бы из сетки. Заливка альфу показывает,
    /// а покраска картинки - нет.
    /// </remarks>
    private static Color OpaqueTint(Color? tint)
    {
        if (!tint.HasValue)
            return Color.white;

        Color color = tint.Value;
        return new Color(color.r, color.g, color.b, 1f);
    }

    /// <summary>
    /// Рисует заливку цветом вместо иконки.
    /// </summary>
    /// <remarks>
    /// Запасной путь для предметов, у которых цвет и есть всё содержимое: заводить
    /// картинку на каждый оттенок ради сетки бессмысленно.
    /// </remarks>
    private static void DrawColorSwatch(Rect rect, Color color)
    {
        float size = Mathf.Min(rect.width, rect.height) - 12f;
        Rect swatch = new(
            rect.x + (rect.width - size) * 0.5f,
            rect.y + (rect.height - size) * 0.5f,
            size,
            size);

        // Шахматка под заливкой: без неё прозрачный цвет не отличить от белого.
        EditorGUI.DrawTextureTransparent(swatch, Texture2D.whiteTexture, ScaleMode.StretchToFill);
        EditorGUI.DrawRect(swatch, color);
        DrawBorder(swatch, Color.Lerp(color, Color.black, 0.35f), 1f);
    }

    private static void DrawSprite(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;
        Rect coordinates = new(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        float spriteAspect = textureRect.width / textureRect.height;
        float rectAspect = rect.width / rect.height;
        if (spriteAspect > rectAspect)
        {
            float height = rect.width / spriteAspect;
            rect.y += (rect.height - height) * 0.5f;
            rect.height = height;
        }
        else
        {
            float width = rect.height * spriteAspect;
            rect.x += (rect.width - width) * 0.5f;
            rect.width = width;
        }

        // Через GUI.color, а не через материал: DrawTextureWithTexCoords красить не умеет,
        // а заводить ради этого материал в редакторе - лишняя сущность.
        Color previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, texture, coordinates, alphaBlend: true);
        GUI.color = previous;

    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private UnityEngine.Object[] GetAvailableAssets(Type elementType)
    {
        if (assetCache.TryGetValue(elementType, out UnityEngine.Object[] cached))
            return cached;

        string[] guids = AssetDatabase.FindAssets($"t:{elementType.Name}", new[] { "Assets" });
        UnityEngine.Object[] assets = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(AssetDatabase.LoadMainAssetAtPath)
            .Where(asset => asset != null && elementType.IsInstanceOfType(asset))
            .Distinct()
            .ToArray();

        assetCache[elementType] = assets;
        return assets;
    }

    private void AddAll(
        SerializedProperty data,
        IReadOnlyCollection<UnityEngine.Object> availableAssets,
        Type elementType)
    {
        var existing = new HashSet<UnityEngine.Object>();
        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object item = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (item != null)
                existing.Add(item);
        }

        Undo.RecordObject(database, $"Add all {elementType.Name} assets");
        int addedCount = 0;
        foreach (UnityEngine.Object asset in availableAssets)
        {
            if (!existing.Add(asset))
                continue;

            int index = data.arraySize;
            data.InsertArrayElementAtIndex(index);
            data.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            addedCount++;
        }

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        Debug.Log($"[PRSDKDatabase] Добавлено {addedCount} assets типа {elementType.Name}.", database);
        GUIUtility.ExitGUI();
    }

    private void RemoveNullReferences(SerializedProperty data)
    {
        Undo.RecordObject(database, "Remove null database entries");
        for (int index = data.arraySize - 1; index >= 0; index--)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue != null)
                continue;

            DeleteArrayElement(data, index);
        }

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private void Clear(SerializedProperty data, Type elementType)
    {
        if (!EditorUtility.DisplayDialog(
                "Очистить базу?",
                $"Все ссылки типа {elementType.Name} будут удалены из этой секции. Сами assets останутся в проекте.",
                "Очистить",
                "Отмена"))
        {
            return;
        }

        Undo.RecordObject(database, $"Clear {elementType.Name} database");
        data.ClearArray();
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        selectedAssets.Clear();
        selections.Clear();
        selectionAnchors.Clear();
        DestroyAllSelectedAssetEditors();
        GUIUtility.ExitGUI();
    }

    private static DatabaseValidationIssue[] GetValidationIssues(
        IDatabaseValidationProvider validationProvider,
        out Exception exception)
    {
        exception = null;
        try
        {
            return validationProvider.Validate()?
                .Where(issue => issue != null)
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.Index)
                .ToArray() ?? Array.Empty<DatabaseValidationIssue>();
        }
        catch (Exception caughtException)
        {
            exception = caughtException;
            return Array.Empty<DatabaseValidationIssue>();
        }
    }

    private static void DrawValidation(
        IReadOnlyList<DatabaseValidationIssue> issues,
        Exception validationException,
        string sectionName,
        int itemCount,
        int availableAssetCount)
    {
        if (validationException != null)
        {
            EditorGUILayout.HelpBox(
                $"Валидатор секции {sectionName} завершился с ошибкой: {validationException.Message}",
                MessageType.Error);
            return;
        }

        if (issues.Count == 0)
        {
            string projectCount = availableAssetCount >= 0
                ? $" Найдено в проекте: {availableAssetCount}."
                : string.Empty;
            string listCount = itemCount >= 0 ? $"В списке: {itemCount}." : "";
            EditorGUILayout.HelpBox(
                $"{listCount}{projectCount} Ошибок валидации нет.",
                MessageType.Info);
            return;
        }

        const int maxVisibleIssues = 10;
        foreach (DatabaseValidationIssue issue in issues.Take(maxVisibleIssues))
        {
            MessageType messageType = issue.Severity switch
            {
                DatabaseValidationSeverity.Error => MessageType.Error,
                DatabaseValidationSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
            EditorGUILayout.HelpBox($"[{issue.Code}] {issue.Message}", messageType);
        }

        if (issues.Count > maxVisibleIssues)
        {
            EditorGUILayout.HelpBox(
                $"Показаны первые {maxVisibleIssues} из {issues.Count} проблем.",
                MessageType.Warning);
        }
    }

    private static int CountMissingAssets(
        SerializedProperty data,
        IEnumerable<UnityEngine.Object> availableAssets)
    {
        var existing = new HashSet<UnityEngine.Object>();
        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object item = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (item != null)
                existing.Add(item);
        }

        return availableAssets.Count(asset => !existing.Contains(asset));
    }

    private static bool HasNullReferences(SerializedProperty data)
    {
        for (int index = 0; index < data.arraySize; index++)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue == null)
                return true;
        }

        return false;
    }

    private static void DeleteArrayElement(SerializedProperty array, int index)
    {
        int oldSize = array.arraySize;
        array.DeleteArrayElementAtIndex(index);
        if (array.arraySize == oldSize)
            array.DeleteArrayElementAtIndex(index);
    }

    private void SetExpanded(bool expanded)
    {
        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serializedDatabase))
            SetExpanded(property, expanded);

        Repaint();
    }

    private static void SetExpanded(SerializedProperty property, bool expanded)
    {
        property.isExpanded = expanded;
        foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(property))
            SetExpanded(child, expanded);
    }

    private bool EnsureDatabase()
    {
        if (database != null && serializedDatabase != null)
            return true;

        BindDatabase();
        return database != null && serializedDatabase != null;
    }

    private void BindDatabase()
    {
        database = PRSDKDatabase.Instance;
        serializedDatabase = database != null ? new SerializedObject(database) : null;
        assetCache.Clear();
    }

    private UnityEditor.Editor GetOrCreateAssetEditor(
        string sectionKey,
        UnityEngine.Object selected)
    {
        if (selectedAssetEditors.TryGetValue(sectionKey, out UnityEditor.Editor editor))
        {
            if (editor != null && editor.target == selected)
                return editor;

            if (editor != null)
                DestroyImmediate(editor);
        }

        editor = UnityEditor.Editor.CreateEditor(selected, typeof(PRSDKEmbeddedAssetEditor));
        selectedAssetEditors[sectionKey] = editor;
        return editor;
    }

    private void DestroySelectedAssetEditor(string sectionKey)
    {
        if (!selectedAssetEditors.TryGetValue(sectionKey, out UnityEditor.Editor editor))
            return;

        selectedAssetEditors.Remove(sectionKey);
        if (editor != null)
            DestroyImmediate(editor);
    }

    private void DestroyAllSelectedAssetEditors()
    {
        foreach (UnityEditor.Editor editor in selectedAssetEditors.Values)
        {
            if (editor != null)
                DestroyImmediate(editor);
        }

        selectedAssetEditors.Clear();
    }

}
