using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отдельное секционное окно <see cref="PRSDKSettings"/> с поиском по модулям.
/// </summary>
public sealed class PRSDKSettingsEditor : EditorWindow
{
    [SerializeField] private PRSDKSettings settings;
    private string search = string.Empty;
    private SerializedObject serializedSettings;
    private Vector2 scrollPosition;

    [MenuItem("PRUnitySDK/Windows/Settings", false, 11)]
    private static void OpenWindow()
    {
        PRSDKSettingsEditor window = GetWindow<PRSDKSettingsEditor>();
        window.titleContent = new GUIContent("SDK Settings");
        window.minSize = new Vector2(620f, 450f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("SDK Settings");
        minSize = new Vector2(620f, 450f);
        BindSettings();
    }

    private void OnGUI()
    {
        if (!EnsureSettings())
        {
            EditorGUILayout.HelpBox("Не найден asset PRSDKSettings.", MessageType.Error);
            return;
        }

        serializedSettings.UpdateIfRequiredOrScript();
        PRSDKInspectorUtility.DrawHeader("PRUnitySDK Settings", settings);
        DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        IReadOnlyList<SerializedProperty> properties =
            PRSDKInspectorUtility.GetRootProperties(serializedSettings);
        int visibleSectionCount = 0;

        foreach (SerializedProperty property in properties)
        {
            string sectionName = PRSDKInspectorUtility.GetSectionName(property);
            if (!PRSDKInspectorUtility.MatchesSearch(sectionName, search))
                continue;

            visibleSectionCount++;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(sectionName),
                    includeChildren: true);
            }

            EditorGUILayout.Space(2f);
        }

        if (visibleSectionCount == 0)
            EditorGUILayout.HelpBox("Секции с таким названием не найдены.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        serializedSettings.ApplyModifiedProperties();
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
            if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                serializedSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("Asset", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }
    }

    private void SetExpanded(bool expanded)
    {
        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serializedSettings))
            property.isExpanded = expanded;

        Repaint();
    }

    private bool EnsureSettings()
    {
        if (settings != null && serializedSettings != null)
            return true;

        BindSettings();
        return settings != null && serializedSettings != null;
    }

    private void BindSettings()
    {
        settings = PRSDKSettings.Instance;
        serializedSettings = settings != null ? new SerializedObject(settings) : null;
    }
}
