using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class LocalizationLangDrawerBase : PropertyDrawer
{
    protected LangType[] languages = Enum.GetValues(typeof(LangType))
        .Cast<LangType>()
        .ToArray();

    private static readonly Dictionary<string, bool> foldouts = new();

    protected abstract SerializedProperty GetValues(SerializedProperty property);

    protected virtual void DrawBeforeLanguages(Rect position, SerializedProperty property, ref Rect r) { }

    protected virtual void DrawAfterLanguages(Rect position, SerializedProperty property, ref Rect r) { }

    protected virtual float GetBeforeLanguagesHeight(SerializedProperty property)
    {
        return 0f;
    }

    protected virtual float GetAfterLanguagesHeight(SerializedProperty property)
    {
        return 0f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var values = GetValues(property);
        EnsureSize(values);

        bool complete = IsComplete(values);

        Rect fullRect = new Rect(
            position.x,
            position.y,
            position.width,
            GetPropertyHeight(property, label)
        );

        // 🎨 BACKGROUND
        EditorGUI.DrawRect(
            fullRect,
            complete
                ? new Color(0.2f, 0.6f, 0.2f, 0.25f)
                : new Color(0.6f, 0.2f, 0.2f, 0.25f)
        );

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 3f;

        Rect r = new Rect(position.x + 6, position.y + 4, position.width - 12, lineH);

        // =========================
        // HEADER
        // =========================
        Rect foldRect = new Rect(r.x, r.y, 20, lineH);
        Rect labelRect = new Rect(r.x + 22, r.y, r.width - 22, lineH);

        bool foldout = GetFoldout(property);

        foldout = EditorGUI.Foldout(foldRect, foldout, GUIContent.none, true);
        SetFoldout(property, foldout);

        EditorGUI.LabelField(labelRect, label.text, EditorStyles.boldLabel);

        r.y += lineH + spacing;

        if (!foldout)
        {
            EditorGUI.EndProperty();
            return;
        }

        // =========================
        // KEY / EXTRA FIELDS (HOOK)
        // =========================
        DrawBeforeLanguages(position, property, ref r);

        DrawSeparator(ref r);

        EditorGUI.indentLevel++;

        // =========================
        // LANG FIELDS
        // =========================
        for (int i = 0; i < languages.Length; i++)
        {
            var element = values.GetArrayElementAtIndex(i);

            float labelWidth = 80f;

            Rect langLabel = new Rect(r.x, r.y, labelWidth, lineH);
            Rect input = new Rect(r.x + labelWidth + 5, r.y, r.width - labelWidth - 5, lineH);

            EditorGUI.LabelField(langLabel, languages[i].ToString());

            element.stringValue = EditorGUI.TextField(input, element.stringValue);

            r.y += lineH + spacing;
        }

        EditorGUI.indentLevel--;

        // =========================
        // FOOTER HOOK (optional)
        // =========================
        DrawAfterLanguages(position, property, ref r);

        EditorGUI.EndProperty();
    }

    protected bool IsComplete(SerializedProperty values)
    {
        for (int i = 0; i < values.arraySize; i++)
        {
            if (string.IsNullOrWhiteSpace(values.GetArrayElementAtIndex(i).stringValue))
                return false;
        }
        return true;
    }

    protected void EnsureSize(SerializedProperty values)
    {
        int target = languages.Length;

        while (values.arraySize < target)
            values.InsertArrayElementAtIndex(values.arraySize);

        while (values.arraySize > target)
            values.DeleteArrayElementAtIndex(values.arraySize - 1);
    }

    protected bool GetFoldout(SerializedProperty property)
    {
        if (!foldouts.TryGetValue(property.propertyPath, out bool value))
            foldouts[property.propertyPath] = true;

        return foldouts[property.propertyPath];
    }

    protected void SetFoldout(SerializedProperty property, bool value)
    {
        foldouts[property.propertyPath] = value;
    }

    protected void DrawSeparator(ref Rect r)
    {
        r.y += 2;
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), new Color(0f, 0f, 0f, 0.25f));
        r.y += 6;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var values = GetValues(property);
        EnsureSize(values);

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 3f;

        bool foldout = foldouts.TryGetValue(property.propertyPath, out bool v) && v;

        float header = lineH + spacing * 2 + 6;

        if (!foldout)
            return header;

        float langPart =
            (languages.Length + 1) * (lineH + spacing) + 10;

        float extra =
            GetBeforeLanguagesHeight(property) +
            GetAfterLanguagesHeight(property);

        return header + langPart + extra;
    }
}