using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LocalizationRow))]
public class LocalizationRowDrawer : PropertyDrawer
{
    private LangType[] languages = Enum.GetValues(typeof(LangType))
        .Cast<LangType>()
        .ToArray();

    private static readonly Dictionary<string, bool> foldouts = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var keyProp = property.FindPropertyRelative(nameof(LocalizationRow.Key).GetBackingField());
        var langProp = property.FindPropertyRelative(nameof(LocalizationRow.LangData).GetBackingField());

        EnsureSize(langProp);

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 3f;

        bool foldout = GetFoldout(property);
        bool complete = IsComplete(langProp, keyProp);

        Rect fullRect = new Rect(position.x, position.y, position.width, GetPropertyHeight(property, label));

        // BACKGROUND
        EditorGUI.DrawRect(
            fullRect,
            complete
                ? new Color(0.2f, 0.6f, 0.2f, 0.25f)
                : new Color(0.6f, 0.2f, 0.2f, 0.25f)
        );

        Rect r = new Rect(position.x + 6, position.y + 4, position.width - 12, lineH);

        // =========================
        // HEADER (FOLDOUT + KEY)
        // =========================

        Rect foldRect = new Rect(r.x, r.y, 20, lineH);
        Rect keyRect = new Rect(r.x + 22, r.y, r.width - 22, lineH);

        foldout = EditorGUI.Foldout(foldRect, foldout, GUIContent.none, true);
        SetFoldout(property, foldout);

        string key = string.IsNullOrWhiteSpace(keyProp.stringValue)
            ? "<No Key>"
            : keyProp.stringValue;

        EditorGUI.LabelField(keyRect, $"Localization: {key}", EditorStyles.boldLabel);

        r.y += lineH + spacing;

        if (!foldout)
        {
            EditorGUI.EndProperty();
            return;
        }

        // =========================
        // KEY FIELD
        // =========================

        keyProp.stringValue = EditorGUI.TextField(r, "Key", keyProp.stringValue);
        r.y += lineH + spacing * 2;

        DrawSeparator(ref r);

        EditorGUI.indentLevel++;

        // =========================
        // LANG FIELDS
        // =========================

        for (int i = 0; i < languages.Length; i++)
        {
            var lang = languages[i];
            var valueProp = langProp.GetArrayElementAtIndex(i);

            valueProp.stringValue = EditorGUI.TextField(
                r,
                ObjectNames.NicifyVariableName(lang.ToString()),
                valueProp.stringValue
            );

            r.y += lineH + spacing;
        }

        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    private bool IsComplete(SerializedProperty langProp, SerializedProperty keyProp)
    {
        if (string.IsNullOrWhiteSpace(keyProp.stringValue))
            return false;

        for (int i = 0; i < langProp.arraySize; i++)
        {
            if (string.IsNullOrWhiteSpace(langProp.GetArrayElementAtIndex(i).stringValue))
                return false;
        }

        return true;
    }

    private bool GetFoldout(SerializedProperty property)
    {
        if (!foldouts.TryGetValue(property.propertyPath, out bool value))
            foldouts[property.propertyPath] = true;

        return foldouts[property.propertyPath];
    }

    private void SetFoldout(SerializedProperty property, bool value)
    {
        foldouts[property.propertyPath] = value;
    }

    private void DrawSeparator(ref Rect r)
    {
        r.y += 2;

        EditorGUI.DrawRect(
            new Rect(r.x, r.y, r.width, 1),
            new Color(0f, 0f, 0f, 0.25f)
        );

        r.y += 6;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var keyProp = property.FindPropertyRelative(nameof(LocalizationRow.Key).GetBackingField());
        var langProp = property.FindPropertyRelative(nameof(LocalizationRow.LangData).GetBackingField());

        EnsureSize(langProp);

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 3f;

        bool foldout = foldouts.TryGetValue(property.propertyPath, out bool v) && v;

        float header = lineH + spacing * 2 + 6;

        if (!foldout)
            return header + 6;

        return (languages.Length + 3) * (lineH + spacing) + 10;
    }

    private void EnsureSize(SerializedProperty langProp)
    {
        while (langProp.arraySize < languages.Length)
            langProp.InsertArrayElementAtIndex(langProp.arraySize);

        while (langProp.arraySize > languages.Length)
            langProp.DeleteArrayElementAtIndex(langProp.arraySize - 1);
    }
}