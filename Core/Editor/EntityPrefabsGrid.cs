using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Сетка префабов, использующих ассет.
/// </summary>
/// <remarks>
/// Описание само по себе мало что говорит: чтобы понять, кого оно называет, приходилось
/// искать по проекту, кто на него ссылается. Блок показывает это сеткой с превью и даёт
/// открыть свойства выбранного префаба, не уходя из окна.
/// <para>
/// Список берётся из <see cref="EntityMetadataUsageIndex"/> - обратной связи «ассет →
/// префабы», которой у Unity нет.
/// </para>
/// </remarks>
public sealed class EntityPrefabsGrid
{
    private const float CardWidth = 92f;
    private const float CardHeight = 112f;
    private const float PreviewSize = 72f;
    private const float Spacing = 6f;

    private Object shownAsset;
    private string selectedPrefabPath;
    private Editor prefabEditor;
    private Object prefabEditorTarget;
    private Vector2 gridScroll;
    private float lastWidth = 300f;
    private float measuredWidth;
    private EditorWindow host;
    private GUIStyle nameStyle;

    /// <summary>
    /// Рисует сетку префабов, ссылающихся на ассет, и свойства выбранного.
    /// </summary>
    /// <param name="asset">Ассет: описание или определение.</param>
    public void Draw(Object asset)
    {
        // Смена описания сбрасывает выбор: показывать префаб от прошлого описания
        // хуже, чем не показывать ничего.
        if (shownAsset != asset)
        {
            shownAsset = asset;
            selectedPrefabPath = null;
            ReleaseEditor();
        }

        AdoptMeasuredWidth();

        // Превью грузятся в фоне, и без перерисовки карточки остались бы пустыми
        // до первого движения мышью. Окно запоминается на будущее: во время отрисовки
        // оно под курсором не всегда.
        host = EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow ?? host;

        IReadOnlyList<string> prefabs = EntityMetadataUsageIndex.GetPrefabs(asset);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Префабы ({prefabs.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("Обновить", GUILayout.Width(90f)))
            {
                EntityMetadataUsageIndex.Invalidate();
                selectedPrefabPath = null;
                ReleaseEditor();

                // Раскладка этого кадра посчитана со старым списком: досматривать её
                // с новым - то самое расхождение проходов, на которое IMGUI ругается.
                GUIUtility.ExitGUI();
            }
        }

        if (prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("Ни один префаб не ссылается на это описание.", MessageType.Info);
            return;
        }

        DrawGrid(prefabs);
        DrawSelectedPrefab();
    }

    /// <summary>
    /// Рисует сетку карточек.
    /// </summary>
    /// <remarks>
    /// Высота ограничена: у популярного описания префабов могут быть десятки, и без
    /// прокрутки сетка вытеснила бы из окна собственно свойства.
    /// </remarks>
    private void DrawGrid(IReadOnlyList<string> prefabs)
    {
        int columns = Mathf.Max(1, Mathf.FloorToInt(MeasureWidth() / (CardWidth + Spacing)));
        int rows = Mathf.CeilToInt(prefabs.Count / (float)columns);
        float height = Mathf.Min(rows * (CardHeight + Spacing) + Spacing, 3f * (CardHeight + Spacing));

        using (var scroll = new EditorGUILayout.ScrollViewScope(gridScroll, GUILayout.Height(height)))
        {
            for (int row = 0; row < rows; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        int index = row * columns + column;

                        if (index >= prefabs.Count)
                            break;

                        DrawCard(prefabs[index]);
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            gridScroll = scroll.scrollPosition;
        }
    }

    /// <summary>
    /// Ширина панели, доступная под сетку.
    /// </summary>
    /// <remarks>
    /// Настоящую ширину раскладка знает только на проходе <c>Repaint</c>, а на <c>Layout</c>
    /// отдаёт ноль. Взять свежее значение прямо здесь нельзя: число колонок разошлось бы
    /// между проходами, а вместе с ним и число нарисованных карточек - IMGUI отвечает на
    /// это исключением про «control N в группе из N».
    /// <para>
    /// Поэтому измеренное значение откладывается и вступает в силу с началом следующего
    /// <c>Layout</c>. Внутри одного кадра оба прохода пользуются одним и тем же числом.
    /// </para>
    /// </remarks>
    private float MeasureWidth()
    {
        Rect area = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint && area.width > 1f)
            measuredWidth = area.width;

        return lastWidth;
    }

    /// <summary>
    /// Принимает измеренную ширину в начале прохода раскладки.
    /// </summary>
    private void AdoptMeasuredWidth()
    {
        if (Event.current.type != EventType.Layout)
            return;

        if (measuredWidth > 1f)
            lastWidth = measuredWidth;
    }

    /// <summary>
    /// Рисует одну карточку префаба.
    /// </summary>
    private void DrawCard(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        bool selected = path == selectedPrefabPath;

        Rect card = GUILayoutUtility.GetRect(
            CardWidth,
            CardHeight,
            GUILayout.Width(CardWidth),
            GUILayout.Height(CardHeight));

        Color accent = new(0.35f, 0.48f, 0.70f, 1f);
        Color background = Color.Lerp(new Color(0.13f, 0.14f, 0.17f, 1f), accent, 0.18f);

        EditorGUI.DrawRect(card, background);
        DrawBorder(card, selected ? accent : Color.Lerp(background, Color.white, 0.14f), selected ? 3f : 1f);

        if (GUI.Button(card, new GUIContent(string.Empty, path), GUIStyle.none))
        {
            selectedPrefabPath = selected ? null : path;
            ReleaseEditor();
            GUIUtility.ExitGUI();
        }

        var previewRect = new Rect(
            card.x + (card.width - PreviewSize) * 0.5f,
            card.y + 6f,
            PreviewSize,
            PreviewSize);

        DrawPreview(previewRect, prefab);

        var labelRect = new Rect(card.x + 4f, previewRect.yMax + 2f, card.width - 8f, 28f);
        GUI.Label(labelRect, Path.GetFileNameWithoutExtension(path), GetNameStyle());
    }

    /// <summary>
    /// Рисует превью префаба.
    /// </summary>
    /// <remarks>
    /// Превью готовится в фоне: пока оно не готово, показывается мелкая иконка типа,
    /// а окно просится на перерисовку - иначе карточка так и осталась бы серой.
    /// </remarks>
    private void DrawPreview(Rect rect, GameObject prefab)
    {
        if (prefab == null)
        {
            GUI.Label(rect, "NULL", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Texture preview = AssetPreview.GetAssetPreview(prefab);

        if (preview != null)
        {
            GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
            return;
        }

        if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
            host?.Repaint();

        Texture thumbnail = AssetPreview.GetMiniThumbnail(prefab);

        if (thumbnail != null)
            GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private GUIStyle GetNameStyle()
    {
        return nameStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };
    }

    /// <summary>
    /// Рисует свойства выбранного префаба.
    /// </summary>
    /// <remarks>
    /// Показывается сущность, а не корневой объект: описание относится к ней, а инспектор
    /// <c>GameObject</c> показал бы имя, слой и теги - всё, кроме нужного. Если сущности
    /// на префабе нет, показывается сам объект: это уже повод удивиться.
    /// </remarks>
    private void DrawSelectedPrefab()
    {
        if (string.IsNullOrEmpty(selectedPrefabPath))
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(selectedPrefabPath);

        if (prefab == null)
        {
            EditorGUILayout.HelpBox("Префаб не открывается - возможно, он удалён.", MessageType.Warning);
            return;
        }

        Object target = prefab.GetComponentInChildren<EntityBase>(true);
        target ??= prefab;

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(selectedPrefabPath, EditorStyles.miniLabel);

            if (GUILayout.Button("Показать", GUILayout.Width(80f)))
                EditorGUIUtility.PingObject(prefab);

            if (GUILayout.Button("Открыть", GUILayout.Width(80f)))
                AssetDatabase.OpenAsset(prefab);
        }

        Editor editor = GetEditor(target);

        if (editor == null)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            editor.OnInspectorGUI();
    }

    private Editor GetEditor(Object target)
    {
        if (prefabEditor != null && prefabEditorTarget == target)
            return prefabEditor;

        ReleaseEditor();

        prefabEditorTarget = target;
        prefabEditor = Editor.CreateEditor(target);

        return prefabEditor;
    }

    /// <summary>
    /// Освобождает встроенный редактор.
    /// </summary>
    public void Dispose()
    {
        ReleaseEditor();
    }

    private void ReleaseEditor()
    {
        if (prefabEditor != null)
            Object.DestroyImmediate(prefabEditor);

        prefabEditor = null;
        prefabEditorTarget = null;
    }
}
