using System;
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

    [MenuItem("PRUnitySDK/Windows/Settings", false, 30)]
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
        bool sectionWasReset = false;

        foreach (SerializedProperty property in properties)
        {
            string sectionName = PRSDKInspectorUtility.GetSectionName(property);
            if (!PRSDKInspectorUtility.MatchesSearch(sectionName, search))
                continue;

            visibleSectionCount++;

            // Сброс меняет данные под руками: дальше по списку идут свойства, собранные
            // до него. Кадр дорисовываем пустым и выходим, следующий нарисует новые.
            if (DrawSection(property, sectionName))
            {
                sectionWasReset = true;
                break;
            }

            EditorGUILayout.Space(2f);
        }

        if (visibleSectionCount == 0)
            EditorGUILayout.HelpBox("Секции с таким названием не найдены.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        if (sectionWasReset)
            return;

        serializedSettings.ApplyModifiedProperties();
    }

    /// <summary>
    /// Рисует один раздел: заголовок с кнопкой сброса, описание и поля.
    /// </summary>
    /// <remarks>
    /// Описание рисуется только у развёрнутого раздела: свёрнутые идут списком, и абзац
    /// текста под каждым превратил бы этот список в стену.
    /// </remarks>
    /// <returns>
    /// <c>true</c>, если раздел сброшен — тогда отрисовку кадра нужно прекратить:
    /// сериализованные данные под руками поменялись.
    /// </returns>
    private bool DrawSection(SerializedProperty property, string sectionName)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Type sectionType = PRSDKInspectorUtility.GetFieldType(typeof(PRSDKSettings), property);

            using (new EditorGUILayout.HorizontalScope())
            {
                property.isExpanded = EditorGUILayout.Foldout(
                    property.isExpanded, sectionName, true, GetHeaderStyle(property));

                if (DrawResetButton(property, sectionName, settings, typeof(PRSDKSettings)))
                    return true;
            }

            if (!property.isExpanded)
                return false;

            PRSDKInspectorUtility.DrawSectionDescription(sectionType);

            using (new EditorGUI.IndentLevelScope())
            {
                if (DrawChildren(property, sectionType, PRSDKInspectorUtility.GetFieldValue(settings, property)))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Стиль заголовка раздела: включённый подсвечивается зелёным.
    /// </summary>
    /// <remarks>
    /// Флагом считается поле раздела с именем вроде <c>Enabled</c>. У свёрнутого списка
    /// разделов это единственный способ увидеть, что работает, а что выключено, не раскрывая
    /// каждый по очереди.
    /// </remarks>
    private static GUIStyle GetHeaderStyle(SerializedProperty property)
    {
        bool highlight = PRSDKInspectorUtility.TryGetSectionToggle(property, out bool isEnabled) && isEnabled;

        return PRSDKInspectorUtility.GetSectionFoldoutStyle(highlight);
    }

    /// <summary>
    /// Рисует поля раздела, разворачивая вложенные разделы со своим описанием.
    /// </summary>
    /// <remarks>
    /// Вложенный раздел — это поле, чей тип помечен <c>SettingsDescription</c>: у опыта
    /// так устроены полоса, значки, фоновое начисление и бустеры. Описание нужно им не
    /// меньше, чем разделу верхнего уровня: именно там лежат числа, которые правят, и по
    /// названию поля не видно, к чему они относятся.
    /// <para>
    /// Остальные поля рисуются обычным <c>PropertyField</c> — вместе со своими
    /// PropertyDrawer-ами, вложенными списками и атрибутами. Разбирать их вручную значило
    /// бы потерять чужую отрисовку.
    /// </para>
    /// </remarks>
    /// <param name="parent">Свойство раздела, чьи поля рисуются.</param>
    /// <param name="parentType">Тип раздела — по нему ищутся поля вложенных.</param>
    /// <param name="parentValue">Значение раздела: владелец вложенных полей при сбросе.</param>
    /// <returns><c>true</c>, если какой-то из разделов был сброшен.</returns>
    private bool DrawChildren(SerializedProperty parent, Type parentType, object parentValue)
    {
        foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(parent))
        {
            Type childType = parentType != null
                ? PRSDKInspectorUtility.GetFieldType(parentType, child)
                : null;

            if (!PRSDKInspectorUtility.HasSectionDescription(childType) || !child.hasVisibleChildren)
            {
                EditorGUILayout.PropertyField(child, includeChildren: true);
                continue;
            }

            if (DrawNestedSection(child, childType, parentValue, parentType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Рисует вложенный раздел: своя шапка с кнопкой сброса, описание и поля.
    /// </summary>
    private bool DrawNestedSection(SerializedProperty property, Type sectionType,
        object owner, Type ownerType)
    {
        string sectionName = PRSDKInspectorUtility.GetSectionName(property);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                property.isExpanded = EditorGUILayout.Foldout(
                    property.isExpanded, sectionName, true, GetHeaderStyle(property));

                if (DrawResetButton(property, sectionName, owner, ownerType))
                    return true;
            }

            if (!property.isExpanded)
                return false;

            PRSDKInspectorUtility.DrawSectionDescription(sectionType);

            using (new EditorGUI.IndentLevelScope())
            {
                object value = owner != null
                    ? PRSDKInspectorUtility.GetFieldValue(owner, property)
                    : null;

                if (DrawChildren(property, sectionType, value))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Кнопка сброса раздела. Перед сбросом правки из полей уходят в ассет, иначе они
    /// вернулись бы поверх сброшенных значений вместе со следующим ApplyModifiedProperties.
    /// </summary>
    /// <param name="owner">Объект, которому принадлежит поле раздела: ассет или раздел-родитель.</param>
    /// <param name="ownerType">Тип владельца — по нему ищется поле.</param>
    private bool DrawResetButton(SerializedProperty property, string sectionName,
        object owner, Type ownerType)
    {
        if (owner == null || ownerType == null)
            return false;

        serializedSettings.ApplyModifiedProperties();

        bool wasReset = PRSDKInspectorUtility.DrawResetSectionButton(
            settings,
            owner,
            sectionName,
            PRSDKInspectorUtility.GetFieldValue(owner, property),
            PRSDKInspectorUtility.GetFieldInfo(ownerType, property));

        if (!wasReset)
            return false;

        serializedSettings.Update();
        Repaint();

        return true;
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
