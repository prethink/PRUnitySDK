using UnityEditor;
using UnityEngine;

/// <summary>
/// Список разделов в левой части окон SDK: строки, заголовки групп, подложка.
/// </summary>
/// <remarks>
/// Окна базы и настроек устроены одинаково — слева список, справа выбранный раздел.
/// Общий список держит их вид одним: строка, отступ и подсветка выбора не расходятся
/// от окна к окну.
/// </remarks>
public static class PRSDKSidebarGUI
{
    /// <summary>
    /// Ширина списка.
    /// </summary>
    public const float Width = 212f;

    private const float RowHeight = 22f;
    private const float CountWidth = 40f;

    private static GUIStyle labelStyle;
    private static GUIStyle selectedStyle;
    private static GUIStyle countStyle;
    private static GUIStyle selectedCountStyle;
    private static GUIStyle groupStyle;

    /// <summary>
    /// Рисует строку раздела.
    /// </summary>
    /// <param name="name">Название раздела.</param>
    /// <param name="depth">Уровень вложенности: ноль — верхний.</param>
    /// <param name="selected">Раздел открыт справа.</param>
    /// <param name="dimmed">Приглушить: в разделе пусто.</param>
    /// <param name="count">Число справа; пусто — не рисовать.</param>
    /// <param name="enabled">Зелёная точка слева: раздел включён.</param>
    /// <returns><c>true</c>, если по строке щёлкнули.</returns>
    public static bool Row(
        string name,
        int depth,
        bool selected,
        bool dimmed = false,
        string count = null,
        bool enabled = false)
    {
        EnsureStyles();

        float indent = GetIndent(depth);
        Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));

        if (selected && Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(
                row,
                EditorGUIUtility.isProSkin
                    ? new Color(0.17f, 0.36f, 0.53f, 1f)
                    : new Color(0.23f, 0.45f, 0.69f, 1f));
        }

        bool clicked = GUI.Button(row, GUIContent.none, GUIStyle.none);

        if (enabled && Event.current.type == EventType.Repaint)
        {
            var dot = new Rect(row.x + indent - 10f, row.y + (row.height - 6f) * 0.5f, 6f, 6f);
            EditorGUI.DrawRect(dot, new Color(0.36f, 0.82f, 0.40f, 1f));
        }

        bool hasCount = !string.IsNullOrEmpty(count);
        var nameRect = new Rect(
            row.x + indent,
            row.y,
            row.width - indent - (hasCount ? CountWidth + 6f : 4f),
            row.height);

        // Пустой раздел приглушён: взгляд сразу находит те, где что-то лежит.
        Color previous = GUI.color;
        if (dimmed && !selected)
            GUI.color = new Color(1f, 1f, 1f, 0.55f);

        GUI.Label(nameRect, name, selected ? selectedStyle : labelStyle);

        if (hasCount)
        {
            var countRect = new Rect(row.xMax - CountWidth - 6f, row.y, CountWidth, row.height);
            GUI.Label(countRect, count, selected ? selectedCountStyle : countStyle);
        }

        GUI.color = previous;
        return clicked;
    }

    /// <summary>
    /// Рисует заголовок группы, который не сворачивается.
    /// </summary>
    public static void Header(string name, int depth = 0)
    {
        EnsureStyles();

        float indent = GetIndent(depth);
        Rect header = GUILayoutUtility.GetRect(0f, RowHeight + 2f, GUILayout.ExpandWidth(true));
        GUI.Label(
            new Rect(header.x + indent, header.y + 6f, header.width - indent, 18f),
            name,
            groupStyle);
    }

    /// <summary>
    /// Рисует заголовок группы со стрелкой.
    /// </summary>
    /// <returns>Группа развёрнута.</returns>
    public static bool FoldoutHeader(string name, bool expanded, string count = null)
    {
        EnsureStyles();

        Rect header = GUILayoutUtility.GetRect(0f, RowHeight + 2f, GUILayout.ExpandWidth(true));

        if (GUI.Button(header, GUIContent.none, GUIStyle.none))
            expanded = !expanded;

        if (Event.current.type == EventType.Repaint)
        {
            var arrow = new Rect(header.x + 4f, header.y + 6f, 14f, 18f);
            EditorStyles.foldout.Draw(arrow, false, false, expanded, false);
        }

        GUI.Label(new Rect(header.x + 18f, header.y + 6f, header.width - 18f - CountWidth, 18f), name, groupStyle);

        if (!string.IsNullOrEmpty(count))
        {
            var countRect = new Rect(header.xMax - CountWidth - 6f, header.y + 6f, CountWidth, 18f);
            GUI.Label(countRect, count, countStyle);
        }

        return expanded;
    }

    /// <summary>
    /// Затемняет подложку списка, отделяя его от содержимого.
    /// </summary>
    public static void Background(Rect rect)
    {
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.14f));
    }

    /// <summary>
    /// Рисует вертикальную черту между списком и содержимым.
    /// </summary>
    public static void Separator()
    {
        Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.4f));
    }

    private static float GetIndent(int depth)
    {
        return 14f + Mathf.Max(0, depth) * 12f;
    }

    private static void EnsureStyles()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip
        };

        selectedStyle = new GUIStyle(labelStyle) { fontStyle = FontStyle.Bold };
        selectedStyle.normal.textColor = Color.white;

        countStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        selectedCountStyle = new GUIStyle(selectedStyle) { alignment = TextAnchor.MiddleRight };
        groupStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleLeft };
    }
}
