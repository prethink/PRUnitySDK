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

        List<SectionGroup> groups = BuildGroups(PRSDKInspectorUtility.GetRootProperties(serializedSettings));
        int visibleSectionCount = 0;
        bool sectionWasReset = false;

        foreach (SectionGroup group in groups)
        {
            visibleSectionCount += group.Sections.Count;

            if (!DrawGroupHeader(group))
                continue;

            foreach ((SerializedProperty property, string sectionName) in group.Sections)
            {
                // Сброс меняет данные под руками: дальше по списку идут свойства, собранные
                // до него. Кадр дорисовываем пустым и выходим, следующий нарисует новые.
                if (DrawSection(property, sectionName))
                {
                    sectionWasReset = true;
                    break;
                }

                EditorGUILayout.Space(2f);
            }

            if (sectionWasReset)
                break;

            EditorGUILayout.Space(6f);
        }

        if (visibleSectionCount == 0)
            EditorGUILayout.HelpBox("Секции с таким названием не найдены.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        if (sectionWasReset)
            return;

        serializedSettings.ApplyModifiedProperties();
    }

    /// <summary>
    /// Разделы одной папки-источника: общей части SDK, закрытой части, проекта игры.
    /// </summary>
    private sealed class SectionGroup
    {
        public string Source;
        public readonly List<(SerializedProperty Property, string Name)> Sections = new();
    }

    private const string GroupExpandedKeyPrefix = "PRSDKSettingsEditor.Group.";

    private static GUIStyle groupHeaderStyle;

    /// <summary>
    /// Раскладывает видимые разделы по папкам, из которых они объявлены.
    /// </summary>
    /// <remarks>
    /// Разделов десятки, и общая часть SDK в одном списке с закрытой и с полями самой игры
    /// не читается: непонятно, что принадлежит чему и что уедет с обновлением какой части.
    /// Поиск действует до группировки, поэтому группа без подходящих разделов не рисуется.
    /// </remarks>
    private List<SectionGroup> BuildGroups(IReadOnlyList<SerializedProperty> properties)
    {
        var groups = new List<SectionGroup>();

        foreach (SerializedProperty property in properties)
        {
            if (HasExternalEditor(property))
                continue;

            string sectionName = PRSDKInspectorUtility.GetSectionName(property);
            if (!PRSDKInspectorUtility.MatchesSearch(sectionName, search))
                continue;

            string source = PRSDKSettingsSources.GetSource(property);
            SectionGroup group = groups.Find(item => item.Source == source);

            if (group == null)
            {
                group = new SectionGroup { Source = source };
                groups.Add(group);
            }

            group.Sections.Add((property, sectionName));
        }

        groups.Sort((left, right) => PRSDKSettingsSources.Compare(left.Source, right.Source));

        return groups;
    }

    /// <summary>
    /// Рисует заголовок группы: имя папки-источника и число разделов в ней.
    /// </summary>
    /// <returns>Группа развёрнута, её разделы нужно рисовать.</returns>
    private bool DrawGroupHeader(SectionGroup group)
    {
        // Во время поиска группы раскрыты: свёрнутая спрятала бы найденное.
        bool searching = !string.IsNullOrWhiteSpace(search);
        string key = GroupExpandedKeyPrefix + group.Source;
        bool expanded = searching || EditorPrefs.GetBool(key, true);

        groupHeaderStyle ??= new GUIStyle(EditorStyles.foldoutHeader) { fontStyle = FontStyle.Bold };

        bool next = EditorGUILayout.Foldout(
            expanded, $"{group.Source}  ({group.Sections.Count})", true, groupHeaderStyle);

        if (!searching && next != expanded)
            EditorPrefs.SetBool(key, next);

        return searching || next;
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
    /// Раздел правится своим окном и здесь не показывается вовсе.
    /// </summary>
    /// <remarks>
    /// Те же данные сырым списком рядом с настоящим редактором только разводят правки,
    /// а строка-ссылка на окно занимала место в списке и ничего не настраивала: своё окно
    /// открывается из меню.
    /// </remarks>
    private static bool HasExternalEditor(SerializedProperty property)
    {
        Type sectionType = PRSDKInspectorUtility.GetFieldType(typeof(PRSDKSettings), property);

        return sectionType != null
            && Attribute.IsDefined(sectionType, typeof(DatabaseExternalEditorAttribute), true);
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
        Dictionary<IPRSettingsFieldOwner, List<string>> moved = null;

        foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(parent))
        {
            // Поле правит другое окно: здесь оно не рисуется, чтобы одно значение
            // не меняли в двух местах. Вместо него останется строка со ссылкой.
            IPRSettingsFieldOwner owner = PRSettingsFieldOwners.Find(parentType, child);

            if (owner != null)
            {
                moved ??= new Dictionary<IPRSettingsFieldOwner, List<string>>();

                if (!moved.TryGetValue(owner, out List<string> names))
                    moved[owner] = names = new List<string>();

                names.Add(PRSDKInspectorUtility.GetSectionName(child));
                continue;
            }

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

        if (moved != null)
            DrawMovedFields(moved);

        return false;
    }

    /// <summary>
    /// Строка на месте полей, которые правит другое окно: что именно и где искать.
    /// </summary>
    private static void DrawMovedFields(Dictionary<IPRSettingsFieldOwner, List<string>> moved)
    {
        foreach (KeyValuePair<IPRSettingsFieldOwner, List<string>> pair in moved)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"{string.Join(", ", pair.Value)} — в окне «{pair.Key.WindowName}»",
                    EditorStyles.wordWrappedMiniLabel);

                if (GUILayout.Button("Открыть", EditorStyles.miniButton, GUILayout.Width(70f)))
                    EditorApplication.ExecuteMenuItem(pair.Key.MenuPath);
            }
        }
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
        {
            if (!HasExternalEditor(property))
                property.isExpanded = expanded;
        }

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
