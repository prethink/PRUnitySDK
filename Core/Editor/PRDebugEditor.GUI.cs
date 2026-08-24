using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    private static GUIStyle summaryMetricStyle;

    private static void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(0.35f, 0.35f, 0.35f));
        EditorGUILayout.Space(3f);
    }

    private void DrawKeyValue(string key, object value)
    {
        if (position.width < 380f)
        {
            EditorGUILayout.LabelField(key, EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(value?.ToString() ?? "-", GUILayout.Height(18f));
            return;
        }

        EditorGUILayout.BeginHorizontal();
        Label(key, 160);
        EditorGUILayout.SelectableLabel(value?.ToString() ?? "-", GUILayout.Height(18f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToggleGrid(params (string Label, bool Value)[] values)
    {
        float availableWidth = Mathf.Min(ContentMaxWidth, Mathf.Max(CompactContentMinWidth, position.width - 28f));
        int columns = availableWidth < 380f ? 2 : availableWidth < 850f ? 3 : 6;
        const float spacing = 4f;
        const float rowHeight = 20f;

        for (int index = 0; index < values.Length; index += columns)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            float cellWidth = (rowRect.width - spacing * (columns - 1)) / columns;
            for (int column = 0; column < columns && index + column < values.Length; column++)
            {
                var value = values[index + column];
                Rect cellRect = new Rect(
                    rowRect.x + column * (cellWidth + spacing),
                    rowRect.y,
                    cellWidth,
                    rowRect.height);
                EditorGUI.ToggleLeft(cellRect, value.Label, value.Value);
            }
        }
    }

    private void DrawSummaryLine(params (string Label, long Value)[] values)
    {
        float availableWidth = Mathf.Max(CompactContentMinWidth, position.width - 28f);
        GUIStyle style = summaryMetricStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            richText = true,
            alignment = TextAnchor.MiddleCenter
        };
        const float spacing = 3f;
        const float horizontalPadding = 12f;
        const float rowHeight = 25f;

        var contents = new GUIContent[values.Length];
        var naturalWidths = new float[values.Length];
        float naturalTotalWidth = spacing * (values.Length - 1);
        for (int index = 0; index < values.Length; index++)
        {
            contents[index] = new GUIContent($"{values[index].Label}: <b>{values[index].Value}</b>");
            naturalWidths[index] = style.CalcSize(contents[index]).x + horizontalPadding;
            naturalTotalWidth += naturalWidths[index];
        }

        int columns = naturalTotalWidth <= availableWidth
            ? values.Length
            : availableWidth < 380f
                ? 2
                : 4;

        for (int index = 0; index < values.Length; index += columns)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            GUI.Box(rowRect, GUIContent.none, EditorStyles.helpBox);

            int rowCount = Mathf.Min(columns, values.Length - index);
            bool useNaturalWidth = columns == values.Length;
            float rowContentWidth = useNaturalWidth
                ? naturalTotalWidth
                : rowRect.width;
            float cursorX = rowRect.x + Mathf.Max(0f, (rowRect.width - rowContentWidth) * 0.5f);
            float equalWidth = (rowRect.width - spacing * (rowCount - 1)) / rowCount;

            for (int column = 0; column < rowCount; column++)
            {
                int valueIndex = index + column;
                float itemWidth = useNaturalWidth ? naturalWidths[valueIndex] : equalWidth;
                Rect itemRect = new Rect(cursorX, rowRect.y + 2f, itemWidth, rowRect.height - 4f);
                GUI.Label(itemRect, contents[valueIndex], style);
                cursorX += itemWidth + spacing;
            }
        }
    }

    private static void DrawFixedRow(bool header, params (string Value, float Width)[] columns)
    {
        EditorGUILayout.BeginHorizontal(header ? EditorStyles.toolbar : GUIStyle.none);
        foreach (var column in columns)
            EditorGUILayout.LabelField(column.Value,
                header ? EditorStyles.miniBoldLabel : EditorStyles.label,
                GUILayout.Width(column.Width));
        EditorGUILayout.EndHorizontal();
    }

    private static void Label(object value, float width) =>
        EditorGUILayout.LabelField(value?.ToString() ?? "-", GUILayout.Width(width));

    private static void DrawIcon(Sprite sprite, float width, float height)
    {
        Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        if (sprite == null || sprite.texture == null)
        {
            GUI.Label(rect, "-", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Rect textureRect = sprite.textureRect;
        float aspect = textureRect.width / textureRect.height;
        Rect drawRect = rect;

        if (aspect > rect.width / rect.height)
        {
            drawRect.height = rect.width / aspect;
            drawRect.y += (rect.height - drawRect.height) * 0.5f;
        }
        else
        {
            drawRect.width = rect.height * aspect;
            drawRect.x += (rect.width - drawRect.width) * 0.5f;
        }

        Rect uv = new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);

        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
    }

    private static void DrawObjectButton(UnityEngine.Object target)
    {
        using (new EditorGUI.DisabledScope(target == null))
        {
            if (!GUILayout.Button("Select", GUILayout.Width(55f)))
                return;

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }
    }

    private static void DrawScriptButton(Type primaryType, Type fallbackType)
    {
        if (!GUILayout.Button("Select", GUILayout.Width(55f)))
            return;

        MonoScript script = FindMonoScript(primaryType) ?? FindMonoScript(fallbackType);
        if (script == null)
        {
            Debug.LogWarning($"Cannot find MonoScript for '{primaryType?.FullName ?? fallbackType?.FullName ?? "<unknown>"}'.");
            return;
        }

        Selection.activeObject = script;
        EditorGUIUtility.PingObject(script);
    }

    private static MonoScript FindMonoScript(Type type)
    {
        if (type == null)
            return null;

        foreach (string guid in AssetDatabase.FindAssets($"{type.Name} t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == type)
                return script;
        }

        return null;
    }

    private static void DrawEmpty(int count, string message)
    {
        if (count == 0)
            EditorGUILayout.HelpBox(message, MessageType.Info);
    }

    private bool MatchesSearch(params object[] values)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        string query = search.Trim();
        return values.Any(value => value?.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string SafeValue(Func<string> getter, string fallback)
    {
        try { return getter?.Invoke() ?? fallback; }
        catch { return fallback; }
    }

    private static string SourceName(object source)
    {
        if (source == null || source.IsNull()) return "<destroyed>";
        if (source is Component component) return $"{component.GetType().Name} ({component.name})";
        if (source is GameObject gameObject) return $"GameObject ({gameObject.name})";
        return source.GetType().Name;
    }

    private static Color DecisionColor(FlagDecision decision) => decision switch
    {
        FlagDecision.Allow => new Color(0.45f, 1f, 0.55f),
        FlagDecision.Deny => new Color(1f, 0.45f, 0.45f),
        _ => Color.gray
    };
}
