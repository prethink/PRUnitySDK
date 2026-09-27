using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Каталог кнопок проекта с превью и инспектором выбранного ассета.
/// </summary>
public sealed class PRSDKButtonsWindow : EditorWindow
{
    /// <summary>
    /// Пункт меню каталога кнопок.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Buttons";

    private const float CardWidth = 124f;
    private const float CardHeight = 152f;
    private const float CardSpacing = 6f;
    private const float PreviewSize = 96f;
    private const float InspectorWidth = 420f;

    [SerializeField] private string search = string.Empty;
    [SerializeField] private string typeFilter = string.Empty;
    [SerializeField] private UIButtonDefinition selected;
    [SerializeField] private Vector2 listScroll;
    [SerializeField] private Vector2 inspectorScroll;

    private UIButtonDefinition[] buttons = Array.Empty<UIButtonDefinition>();
    private string[] typeNames = { "Все" };
    private Editor inspector;
    private GUIStyle nameStyle;
    private bool reloadPending = true;
    private float gridWidth = 300f;
    private float measuredGridWidth;

    [MenuItem(MenuPath, false, 17)]
    private static void Open()
    {
        GetWindow<PRSDKButtonsWindow>().Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Кнопки HUD");
        minSize = new Vector2(720f, 480f);
        reloadPending = true;
        if (selected == null && Selection.activeObject is UIButtonDefinition definition)
            selected = definition;
        Undo.undoRedoPerformed += RequestReload;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= RequestReload;
        ReleaseInspector();
    }

    private void OnProjectChange() => RequestReload();

    private void OnSelectionChange()
    {
        if (Selection.activeObject is UIButtonDefinition definition)
        {
            Select(definition);
            Repaint();
        }
    }

    private void RequestReload()
    {
        reloadPending = true;
        ReleaseInspector();
        Repaint();
    }

    private void OnGUI()
    {
        // Количество карточек должно совпадать между Layout и Repaint.
        if (Event.current.type == EventType.Layout)
        {
            if (reloadPending)
                Reload();
            if (measuredGridWidth > 1f)
                gridWidth = measuredGridWidth;
        }

        DrawToolbar();
        UIButtonDefinition[] visible = buttons.Where(MatchesFilter).ToArray();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawGrid(visible);
            DrawInspector();
        }
    }

    private void Reload()
    {
        ReleaseInspector();
        buttons = AssetDatabase.FindAssets($"t:{nameof(UIButtonDefinition)}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<UIButtonDefinition>)
            .Where(button => button != null)
            .OrderBy(button => button.name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
            .ToArray();
        typeNames = new[] { "Все" }
            .Concat(buttons.Select(button => button.GetType().Name)
                .Distinct(StringComparer.Ordinal).OrderBy(type => type, StringComparer.Ordinal))
            .ToArray();
        if (!typeNames.Contains(typeFilter))
            typeFilter = string.Empty;
        if (selected != null && !buttons.Contains(selected))
            selected = null;
        reloadPending = false;
    }

    private void DrawToolbar()
    {
        bool changed;
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField($"Кнопок: {buttons.Length}", GUILayout.Width(95f));
            EditorGUI.BeginChangeCheck();
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
            int index = Mathf.Max(0, Array.IndexOf(typeNames, typeFilter));
            index = EditorGUILayout.Popup(index, typeNames, EditorStyles.toolbarPopup, GUILayout.Width(160f));
            typeFilter = index > 0 ? typeNames[index] : string.Empty;
            changed = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("Создать", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                CreateButton();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                RequestReload();
                GUIUtility.ExitGUI();
            }
        }
        if (changed)
            GUIUtility.ExitGUI();
    }

    private bool MatchesFilter(UIButtonDefinition definition)
    {
        if (definition == null || !string.IsNullOrEmpty(typeFilter) && definition.GetType().Name != typeFilter)
            return false;
        string query = search.Trim();
        return query.Length == 0 || Contains(definition.name, query)
            || Contains(AssetDatabase.GetAssetPath(definition), query)
            || Contains(definition.LocalizationKey, query)
            || definition.LocalizationValues != null && definition.LocalizationValues.Values.Any(value => Contains(value, query));
    }

    private static bool Contains(string value, string query) =>
        value != null && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    private void DrawGrid(UIButtonDefinition[] visible)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (visible.Length == 0)
            {
                EditorGUILayout.HelpBox("Кнопок с такими условиями не найдено. Новую кнопку можно создать в панели сверху.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }
            using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll))
            {
                Rect measure = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint && measure.width > 1f)
                    measuredGridWidth = measure.width;
                int columns = Mathf.Max(1, Mathf.FloorToInt(gridWidth / (CardWidth + CardSpacing)));
                for (int start = 0; start < visible.Length; start += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int column = 0; column < columns && start + column < visible.Length; column++)
                            DrawCard(visible[start + column]);
                        GUILayout.FlexibleSpace();
                    }
                }
                listScroll = scroll.scrollPosition;
            }
        }
    }

    private void DrawCard(UIButtonDefinition definition)
    {
        Rect card = GUILayoutUtility.GetRect(CardWidth, CardHeight, GUILayout.Width(CardWidth), GUILayout.Height(CardHeight));
        EditorGUI.DrawRect(card, selected == definition ? new Color(0.2f, 0.38f, 0.57f) : new Color(0.16f, 0.17f, 0.2f));
        if (GUI.Button(card, new GUIContent(string.Empty, AssetDatabase.GetAssetPath(definition)), GUIStyle.none))
        {
            Select(definition);
            GUIUtility.ExitGUI();
        }
        var preview = new Rect(card.x + (card.width - PreviewSize) * 0.5f, card.y + 6f, PreviewSize, PreviewSize);
        // Все слои помещаются в карточке, сохраняя заданные размеры относительно подложки.
        float largestSize = Mathf.Max(1f, Mathf.Max(
            definition.Decoration != null ? definition.DecorationSize : 0f,
            definition.Icon != null ? definition.IconSize : 0f));
        Rect plateRect = SizedRect(preview, 1f / largestSize);
        if (definition.Background != null)
            DrawSprite(plateRect, definition.Background, definition.Color);
        else
            EditorGUI.DrawRect(plateRect, definition.Color);
        DrawSprite(SizedRect(plateRect, definition.DecorationSize), definition.Decoration, Color.white);
        DrawSprite(SizedRect(plateRect, definition.IconSize), definition.Icon, Color.white);
        if (definition.Icon == null && definition.Decoration == null)
            GUI.Label(preview, "Нет иконки", EditorStyles.centeredGreyMiniLabel);
        nameStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true };
        GUI.Label(new Rect(card.x + 4f, preview.yMax + 2f, card.width - 8f, 44f), definition.name, nameStyle);
    }

    private static Rect SizedRect(Rect rect, float size)
    {
        Vector2 dimensions = rect.size * size;
        return new Rect(rect.center - dimensions * 0.5f, dimensions);
    }

    private void DrawSprite(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || rect.width <= 0f || rect.height <= 0f)
            return;
        Texture preview = AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite);
        if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID()))
            Repaint();
        if (preview == null)
            return;
        Color previous = GUI.color;
        try
        {
            GUI.color = previous * tint;
            GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
        }
        finally
        {
            GUI.color = previous;
        }
    }

    private void DrawInspector()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(InspectorWidth)))
        {
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Выберите кнопку слева.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(selected.name, EditorStyles.largeLabel);
                if (GUILayout.Button("Показать", GUILayout.Width(75f)))
                    EditorGUIUtility.PingObject(selected);
                using (new EditorGUI.DisabledScope(!EditorUtility.IsDirty(selected)))
                {
                    if (GUILayout.Button("Сохранить", GUILayout.Width(85f)))
                        AssetDatabase.SaveAssetIfDirty(selected);
                }
            }
            EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selected), EditorStyles.miniLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            using (var scroll = new EditorGUILayout.ScrollViewScope(inspectorScroll))
            {
                if (inspector == null || inspector.target != selected)
                {
                    ReleaseInspector();
                    inspector = Editor.CreateEditor(selected);
                }
                EditorGUI.BeginChangeCheck();
                inspector.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                    Repaint();
                inspectorScroll = scroll.scrollPosition;
            }
        }
    }

    private void Select(UIButtonDefinition definition)
    {
        if (selected == definition)
            return;
        selected = definition;
        inspectorScroll = Vector2.zero;
        ReleaseInspector();
    }

    private void CreateButton()
    {
        string folder = selected != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(selected)) : "Assets";
        string path = EditorUtility.SaveFilePanelInProject("Новая кнопка HUD", "UIButton", "asset",
            "Выберите, где сохранить определение кнопки.", folder);
        if (string.IsNullOrEmpty(path))
            return;
        var definition = CreateInstance<UIButtonDefinition>();
        AssetDatabase.CreateAsset(definition, path);
        search = string.Empty;
        typeFilter = string.Empty;
        Select(definition);
        Selection.activeObject = definition;
        RequestReload();
        EditorGUIUtility.PingObject(definition);
    }

    private void ReleaseInspector()
    {
        if (inspector != null)
            DestroyImmediate(inspector);
        inspector = null;
    }
}
