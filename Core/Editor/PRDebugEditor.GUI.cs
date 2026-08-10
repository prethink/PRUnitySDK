using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    private static void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(0.35f, 0.35f, 0.35f));
        EditorGUILayout.Space(3f);
    }

    private static void DrawKeyValue(string key, object value)
    {
        EditorGUILayout.BeginHorizontal();
        Label(key, 160);
        EditorGUILayout.SelectableLabel(value?.ToString() ?? "-", GUILayout.Height(18f));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawToggleGrid(params (string Label, bool Value)[] values)
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var value in values)
            EditorGUILayout.ToggleLeft(value.Label, value.Value, GUILayout.Width(100f));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawCards(params (string Label, long Value)[] values)
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var value in values)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(75f));
            EditorGUILayout.LabelField(value.Label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value.Value.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
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
