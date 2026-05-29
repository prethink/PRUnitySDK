using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LocalizationWindow : ExtendedEditorWindow
{
    private SerializedObject so;

    private SerializedProperty commonProp;
    private SerializedProperty projectProp;

    private HashSet<string> duplicateKeys = new HashSet<string>();

    private Vector2 scroll;
    private string search;

    private LangType[] languages;

    [MenuItem("PRUnitySDK/Tools/Localization")]
    public static void Open()
    {
        GetWindow<LocalizationWindow>("Localization");
    }

    private void OnEnable()
    {
        database = ScriptableObjectSingleton<PRSDKDatabase>.Instance;
        so = new SerializedObject(database);

        var loc = so.FindProperty(nameof(PRUnitySDK.Database.LocalizationDatabase).GetBackingField());

        commonProp = loc.FindPropertyRelative(nameof(PRUnitySDK.Database.LocalizationDatabase.Common).GetBackingField());
        projectProp = loc.FindPropertyRelative(nameof(PRUnitySDK.Database.LocalizationDatabase.Project).GetBackingField());

        languages = Enum.GetValues(typeof(LangType))
            .Cast<LangType>()
            .ToArray();
    }

    private void OnGUI()
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("PRSDKDatabase not found", MessageType.Error);
            return;
        }

        so.Update();

        Tabs(
            ("Common", () => DrawTable(commonProp)),
            ("Project", () => DrawTable(projectProp))
        );

        so.ApplyModifiedProperties();
    }

    private void DrawTopWarning()
    {
        if (duplicateKeys.Count == 0)
            return;

        EditorGUILayout.Space(5);

        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "⚠ Duplicate localization keys detected!",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            string.Join(", ", duplicateKeys)
        );

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = oldColor;

        EditorGUILayout.Space(5);
    }

    private void ValidateDuplicates(SerializedProperty listProp)
    {
        duplicateKeys.Clear();

        var keys = new HashSet<string>();
        var duplicates = new HashSet<string>();

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            var key = element.FindPropertyRelative(nameof(LocalizationRow.LocalizationKey).GetBackingField()).stringValue;

            if (string.IsNullOrEmpty(key))
                continue;

            if (!keys.Add(key))
                duplicates.Add(key);
        }

        duplicateKeys = duplicates;
    }

    // =========================
    // TABLE
    // =========================

    private void DrawTable(SerializedProperty listProp)
    {
        ValidateDuplicates(listProp);
        DrawTopWarning();
        DrawSearch();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);

            var keyProp = element.FindPropertyRelative(nameof(LocalizationRow.LocalizationKey).GetBackingField());
            var langProp = element.FindPropertyRelative(LocalizationRow.LangPropertyName);

            if (!Filter(keyProp.stringValue))
                continue;

            EnsureLangSize(langProp);

            DrawRow(listProp, element, i, keyProp, langProp);
        }

        EditorGUILayout.EndScrollView();

        DrawAdd(listProp);
    }

    // =========================
    // SEARCH
    // =========================

    private void DrawSearch()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        search = GUILayout.TextField(search, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            search = "";

        EditorGUILayout.EndHorizontal();
    }

    private bool Filter(string key)
    {
        if (string.IsNullOrEmpty(search))
            return true;

        return key != null &&
               key.ToLower().Contains(search.ToLower());
    }

    // =========================
    // HEADER
    // =========================

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal("box");

        GUILayout.Label("Key", GUILayout.Width(200));

        foreach (var lang in languages)
        {
            GUILayout.Label(GetLangName(lang), GUILayout.Width(150));
        }

        GUILayout.Label("", GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
    }

    // =========================
    // ROW
    // =========================

    private void DrawRow(
        SerializedProperty listProp,
        SerializedProperty element,
        int index,
        SerializedProperty keyProp,
        SerializedProperty langProp)
    {
        EditorGUILayout.BeginHorizontal("box");

        // KEY
        keyProp.stringValue = EditorGUILayout.TextField(
            keyProp.stringValue,
            GUILayout.Width(200)
        );

        // LANGS
        for (int i = 0; i < languages.Length; i++)
        {
            var valueProp = langProp.GetArrayElementAtIndex(i);

            valueProp.stringValue = EditorGUILayout.TextField(
                valueProp.stringValue,
                GUILayout.Width(150)
            );
        }

        GUILayout.FlexibleSpace();

        // ACTIONS
        if (GUILayout.Button("Copy", GUILayout.Width(30)))
        {
            so.Update();

            listProp.InsertArrayElementAtIndex(index + 1);

            var src = listProp.GetArrayElementAtIndex(index);
            var dst = listProp.GetArrayElementAtIndex(index + 1);

            // KEY
            dst.FindPropertyRelative(nameof(LocalizationRow.LocalizationKey).GetBackingField()).stringValue =
                src.FindPropertyRelative(nameof(LocalizationRow.LocalizationKey).GetBackingField()).stringValue + "_copy";

            // LANG DATA
            var srcLang = src.FindPropertyRelative(LocalizationRow.LangPropertyName);
            var dstLang = dst.FindPropertyRelative(LocalizationRow.LangPropertyName);

            dstLang.ClearArray();

            for (int i = 0; i < srcLang.arraySize; i++)
            {
                dstLang.InsertArrayElementAtIndex(i);
                dstLang.GetArrayElementAtIndex(i).stringValue =
                    srcLang.GetArrayElementAtIndex(i).stringValue;
            }

            so.ApplyModifiedProperties();

            Repaint();
        }

        if (GUILayout.Button("X", GUILayout.Width(30)))
        {
            if (!EditorUtility.DisplayDialog(
                "Delete",
                $"Delete key '{keyProp.stringValue}'?",
                "Yes",
                "No"))
                return;

            so.Update();
            listProp.DeleteArrayElementAtIndex(index);
            so.ApplyModifiedProperties();

            GUIUtility.ExitGUI(); // 🔥 ВАЖНО
            return;
        }

        EditorGUILayout.EndHorizontal();
    }

    // =========================
    // ADD ROW
    // =========================

    private void DrawAdd(SerializedProperty listProp)
    {
        EditorGUILayout.Space(5);

        if (GUILayout.Button("+ Add Key", GUILayout.Height(25)))
        {
            listProp.InsertArrayElementAtIndex(listProp.arraySize);

            var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);

            newElement.FindPropertyRelative(nameof(LocalizationRow.LocalizationKey).GetBackingField()).stringValue = "new_key";

            var langProp = newElement.FindPropertyRelative(LocalizationRow.LangPropertyName);
            EnsureLangSize(langProp);
        }
    }

    // =========================
    // HELPERS
    // =========================

    private void EnsureLangSize(SerializedProperty langProp)
    {
        int target = languages.Length;

        while (langProp.arraySize < target)
            langProp.InsertArrayElementAtIndex(langProp.arraySize);

        while (langProp.arraySize > target)
            langProp.DeleteArrayElementAtIndex(langProp.arraySize - 1);
    }

    private string GetLangName(LangType lang)
    {
        var mem = typeof(LangType).GetMember(lang.ToString());
        var attr = mem[0]
            .GetCustomAttributes(typeof(InspectorNameAttribute), false)
            .FirstOrDefault() as InspectorNameAttribute;

        return attr != null ? attr.displayName : lang.ToString();
    }
}