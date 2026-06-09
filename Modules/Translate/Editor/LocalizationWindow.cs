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

        var loc = so.FindProperty(
            nameof(PRUnitySDK.Database.LocalizationDatabase).GetBackingField()
        );

        commonProp = loc.FindPropertyRelative(
            nameof(PRUnitySDK.Database.LocalizationDatabase.Common).GetBackingField()
        );

        projectProp = loc.FindPropertyRelative(
            nameof(PRUnitySDK.Database.LocalizationDatabase.Project).GetBackingField()
        );

        languages = Enum.GetValues(typeof(LangType))
            .Cast<LangType>()
            .ToArray();
    }

    private void OnGUI()
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("Database not found", MessageType.Error);
            return;
        }

        so.Update();

        Tabs(("Common", () => DrawList(commonProp)),("Project", () => DrawList(projectProp)));

        so.ApplyModifiedProperties();
    }

    // =========================
    // DRAW LIST
    // =========================

    private void DrawList(SerializedProperty listProp)
    {
        DrawSearch();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            element.isExpanded = true;
            if (!Filter(element))
                continue;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.PropertyField(element, GUIContent.none, true);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete",
                    $"Delete '{element.FindPropertyRelative(nameof(LocalizationControl.LocalizationKey).GetBackingField()).stringValue}'?",
                    "Yes",
                    "No"))
                {
                    so.Update();
                    listProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        DrawAdd(listProp);
    }

    // =========================
    // ADD
    // =========================

    private void DrawAdd(SerializedProperty listProp)
    {
        bool isSearching = !string.IsNullOrEmpty(search);

        GUI.enabled = !isSearching;

        if (GUILayout.Button("+ Add Key"))
        {
            so.Update();

            int index = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(index);

            var element = listProp.GetArrayElementAtIndex(index);

            // =========================
            // FORCE RESET OBJECT STATE
            // =========================

            // KEY (ВАЖНО: не backing field!)
            var keyProp = element.FindPropertyRelative(nameof(LocalizationControl.LocalizationKey).GetBackingField());
            if (keyProp != null)
                keyProp.stringValue = "new_key";

            // DICTIONARY RESET
            var dictProp = element.FindPropertyRelative(LocalizationControl.InternalLocalizationValuesPropertyName.GetBackingField());

            if (dictProp != null)
            {
                var list = dictProp.FindPropertyRelative("_serializedList");

                if (list != null)
                    list.ClearArray();
            }

            so.ApplyModifiedProperties();
        }

        GUI.enabled = true;
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

    private bool Filter(SerializedProperty element)
    {
        if (string.IsNullOrEmpty(search))
            return true;

        var keyProp = element.FindPropertyRelative(nameof(LocalizationControl.LocalizationKey).GetBackingField());

        if (keyProp.stringValue != null &&
            keyProp.stringValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var dict = element
            .FindPropertyRelative(LocalizationControl.InternalLocalizationValuesPropertyName.GetBackingField())
            .FindPropertyRelative("_serializedList");

        for (int i = 0; i < dict.arraySize; i++)
        {
            var v = dict.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("Value");

            if (v.stringValue != null &&
                v.stringValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    // =========================
    // HELPERS
    // =========================

    private void EnsureLangSize(SerializedProperty dictList)
    {
        while (dictList.arraySize < languages.Length)
            dictList.InsertArrayElementAtIndex(dictList.arraySize);
    }

    private SerializedProperty FindPair(SerializedProperty list, LangType lang)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            var element = list.GetArrayElementAtIndex(i);

            var keyProp = element.FindPropertyRelative("Key");

            if ((LangType)keyProp.enumValueIndex == lang)
                return element;
        }

        return null;
    }
}