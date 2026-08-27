using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отдельное окно <see cref="PRSDKDatabase"/> с управлением каталогами definitions.
/// </summary>
public sealed class PRSDKDatabaseEditor : EditorWindow
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
    private readonly Dictionary<string, UnityEditor.Editor> selectedAssetEditors = new();
    [SerializeField] private PRSDKDatabase database;
    [SerializeField] private float gridSplit = 0.58f;
    private string search = string.Empty;
    private SerializedObject serializedDatabase;
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

    [MenuItem("PRUnitySDK/Windows/Database", false, 10)]
    private static void OpenWindow()
    {
        PRSDKDatabaseEditor window = GetWindow<PRSDKDatabaseEditor>();
        window.titleContent = new GUIContent("SDK Database");
        window.minSize = new Vector2(620f, 450f);
        window.Show();
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

    private void OnGUI()
    {
        if (!EnsureDatabase())
        {
            EditorGUILayout.HelpBox("Не найден asset PRSDKDatabase.", MessageType.Error);
            return;
        }

        serializedDatabase.UpdateIfRequiredOrScript();
        PRSDKInspectorUtility.DrawHeader("PRUnitySDK Database", database);
        DrawToolbar();

        IReadOnlyList<SerializedProperty> properties =
            PRSDKInspectorUtility.GetRootProperties(serializedDatabase);
        int visibleSectionCount = 0;
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            foreach (SerializedProperty property in properties)
            {
                string sectionName = PRSDKInspectorUtility.GetSectionName(property);
                if (!PRSDKInspectorUtility.MatchesSearch(sectionName, search))
                    continue;

                visibleSectionCount++;
                DrawSection(property, sectionName);
            }

            if (visibleSectionCount == 0)
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
            if (GUILayout.Button("Asset", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
        }
    }

    private void DrawSection(SerializedProperty property, string sectionName)
    {
        Type fieldType = PRSDKInspectorUtility.GetFieldType(database.GetType(), property);
        object sectionValue = PRSDKInspectorUtility.GetFieldValue(database, property);
        DrawSection(property, sectionName, fieldType, sectionValue);
    }

    private void DrawSection(
        SerializedProperty property,
        string sectionName,
        Type fieldType,
        object sectionValue)
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

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (useGrid || hasNestedDatabases)
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
        return supportsAssetTools &&
               (options.Presentation == DatabaseEditorPresentation.Grid ||
                options.Presentation == DatabaseEditorPresentation.Auto &&
                typeof(ItemDefinitionBase).IsAssignableFrom(elementType));
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
                DrawAssetGridToolbar(viewState, visibleEntries.Length, data.arraySize);

                Vector2 gridScroll = GetScrollPosition(gridScrollPositions, sectionKey);
                using (var gridScrollView = new EditorGUILayout.ScrollViewScope(
                           gridScroll,
                           GUILayout.ExpandHeight(true)))
                {
                    DrawAssetCards(sectionKey, visibleEntries, selected, leftWidth);
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
        int totalCount)
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
        UnityEngine.Object selected,
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
                        entry.Asset,
                        entry.Asset == selected,
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
        UnityEngine.Object asset,
        bool selected,
        bool isInvalid)
    {
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
        if (GUI.Button(cardRect, new GUIContent(string.Empty, tooltip), GUIStyle.none))
        {
            selectedAssets[sectionKey] = asset;
            Repaint();
        }

        Rect iconRect = new(cardRect.x + 8f, cardRect.y + 8f, cardRect.width - 16f, 88f);
        Sprite icon = asset is IIconProvider iconProvider ? iconProvider.Icon : null;
        Color? tint = ResolveIconTint(asset);

        if (icon != null)
            DrawSprite(iconRect, icon, OpaqueTint(tint));
        else if (tint.HasValue)
            DrawColorSwatch(iconRect, tint.Value);
        else
            GUI.Label(iconRect, asset == null ? "NULL" : "Нет иконки", EditorStyles.centeredGreyMiniLabel);

        Rect labelRect = new(cardRect.x + 6f, iconRect.yMax + 5f, cardRect.width - 12f, 34f);
        GUI.Label(labelRect, GetAssetName(asset), cardNameStyle);

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

        if (GUILayout.Button("Удалить из базы"))
            RemoveAsset(sectionKey, data, selected);

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

    private void RemoveAsset(
        string sectionKey,
        SerializedProperty data,
        UnityEngine.Object asset)
    {
        for (int index = 0; index < data.arraySize; index++)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue != asset)
                continue;

            Undo.RecordObject(database, "Remove database asset");
            DeleteArrayElement(data, index);
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            selectedAssets.Remove(sectionKey);
            DestroySelectedAssetEditor(sectionKey);
            GUIUtility.ExitGUI();
            return;
        }
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
        cardNameStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
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
