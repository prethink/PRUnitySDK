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
    private Vector2 sidebarScroll;

    /// <summary>
    /// Раздел, открытый в правой части окна.
    /// </summary>
    /// <remarks>
    /// Сериализуется: после перезагрузки скриптов окно возвращается к тому же разделу.
    /// </remarks>
    [SerializeField] private string selectedSection;

    /// <summary>
    /// Раздел, показанный справа в этом кадре.
    /// </summary>
    /// <remarks>
    /// Отличается от выбранного, пока идёт поиск и выбранный под запрос не подходит.
    /// </remarks>
    [NonSerialized] private string shownSection;

    /// <summary>
    /// На сколько уровней вглубь поиск заглядывает в поля раздела.
    /// </summary>
    private const int SearchDepth = 4;

    [MenuItem("PRUnitySDK/Windows/Settings", false, 30)]
    private static void OpenWindow()
    {
        PRSDKSettingsEditor window = GetWindow<PRSDKSettingsEditor>();
        window.titleContent = new GUIContent("SDK Settings");
        window.minSize = new Vector2(780f, 450f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("SDK Settings");
        minSize = new Vector2(780f, 450f);
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
        DrawToolbar();

        List<SectionGroup> groups = BuildGroups(PRSDKInspectorUtility.GetRootProperties(serializedSettings));
        bool found = TryResolveSelected(groups, out SerializedProperty selected, out string selectedName);
        bool sectionWasReset = false;

        // Слева список разделов, справа один раздел целиком. Стопка сворачиваемых разделов
        // заставляла искать нужный среди полусотни строк, а развёрнутый уезжал под прокрутку.
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
        {
            DrawSidebar(groups);
            PRSDKSidebarGUI.Separator();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (found)
                sectionWasReset = DrawSection(selected, selectedName);
            else
                EditorGUILayout.HelpBox("Разделы с таким названием или полем не найдены.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        // Сброс меняет данные под руками: правки из полей этого кадра применять уже нельзя.
        if (sectionWasReset)
            return;

        serializedSettings.ApplyModifiedProperties();
    }

    /// <summary>
    /// Находит открытый раздел, а если фильтр его убрал — берёт первый из найденных.
    /// </summary>
    private bool TryResolveSelected(
        List<SectionGroup> groups,
        out SerializedProperty property,
        out string sectionName)
    {
        property = null;
        sectionName = null;

        foreach (SectionGroup group in groups)
        {
            foreach ((SerializedProperty candidate, string name, _) in group.Sections)
            {
                if (candidate.propertyPath == selectedSection)
                {
                    property = candidate;
                    sectionName = name;
                    shownSection = selectedSection;
                    return true;
                }

                if (property == null)
                {
                    property = candidate;
                    sectionName = name;
                }
            }
        }

        if (property == null)
        {
            shownSection = null;
            return false;
        }

        shownSection = property.propertyPath;

        // Поиск показывает найденное, но выбор не переписывает: стёрли запрос —
        // окно возвращается к разделу, который был открыт до поиска.
        if (string.IsNullOrWhiteSpace(search))
            selectedSection = shownSection;

        return true;
    }

    /// <summary>
    /// Рисует список разделов по папкам-источникам.
    /// </summary>
    /// <remarks>
    /// Включённый раздел помечен зелёной точкой: по списку видно, что работает, не открывая
    /// разделы по очереди. Раздел без полей приглушён. Иконка раздела задаётся в его
    /// <see cref="SettingsDescriptionAttribute.Icon"/>.
    /// </remarks>
    private void DrawSidebar(List<SectionGroup> groups)
    {
        // Во время поиска группы раскрыты: свёрнутая спрятала бы найденное.
        bool searching = !string.IsNullOrWhiteSpace(search);

        using (var sidebar = new EditorGUILayout.VerticalScope(
                   GUILayout.Width(PRSDKSidebarGUI.Width),
                   GUILayout.ExpandHeight(true)))
        {
            PRSDKSidebarGUI.Background(sidebar.rect);
            sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll, GUILayout.ExpandHeight(true));

            foreach (SectionGroup group in groups)
            {
                string key = GroupExpandedKeyPrefix + group.Source;
                bool expanded = searching || EditorPrefs.GetBool(key, true);
                bool next = PRSDKSidebarGUI.FoldoutHeader(group.Source, expanded, group.Sections.Count.ToString());

                if (!searching && next != expanded)
                    EditorPrefs.SetBool(key, next);

                if (!searching && !next)
                    continue;

                foreach ((SerializedProperty property, string sectionName, Texture icon) in group.Sections)
                {
                    bool isOn = PRSDKInspectorUtility.TryGetSectionToggle(property, out bool isEnabled) && isEnabled;
                    bool clicked = PRSDKSidebarGUI.Row(
                        sectionName,
                        1,
                        property.propertyPath == shownSection,
                        dimmed: !property.hasVisibleChildren,
                        enabled: isOn,
                        icon: icon);

                    if (!clicked)
                        continue;

                    selectedSection = property.propertyPath;
                    scrollPosition = Vector2.zero;

                    // Поле, в котором стоял курсор, принадлежало прежнему разделу.
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            if (groups.Count == 0)
                GUILayout.Label("Ничего не найдено", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// Раздел подходит под поиск названием или названием одного из своих полей.
    /// </summary>
    /// <remarks>
    /// Ищут обычно настройку, а не раздел: «volume» должно привести к звуку, даже если
    /// раздел называется иначе.
    /// </remarks>
    private bool MatchesSearch(SerializedProperty property, string sectionName)
    {
        return PRSDKInspectorUtility.MatchesSearch(sectionName, search)
            || MatchesFields(property, SearchDepth);
    }

    /// <summary>
    /// Ищет запрос в названиях полей раздела, заходя во вложенные разделы.
    /// </summary>
    /// <remarks>
    /// Числа, которые правят, лежат глубже первого уровня: бустеры — внутри опыта.
    /// В списки поиск не заходит: в них сотни элементов с одинаковыми названиями полей,
    /// а обход идёт на каждую перерисовку окна.
    /// </remarks>
    private bool MatchesFields(SerializedProperty property, int depth)
    {
        foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(property))
        {
            if (PRSDKInspectorUtility.MatchesSearch(child.displayName, search))
                return true;

            if (depth <= 1
                || child.isArray
                || child.propertyType != SerializedPropertyType.Generic
                || !child.hasVisibleChildren)
            {
                continue;
            }

            if (MatchesFields(child, depth - 1))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Разделы одной папки-источника: общей части SDK, закрытой части, проекта игры.
    /// </summary>
    private sealed class SectionGroup
    {
        public string Source;
        public readonly List<(SerializedProperty Property, string Name, Texture Icon)> Sections = new();
    }

    private const string GroupExpandedKeyPrefix = "PRSDKSettingsEditor.Group.";

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
            Type sectionType = PRSDKInspectorUtility.GetFieldType(typeof(PRSDKSettings), property);

            if (HasExternalEditor(sectionType))
                continue;

            string sectionName = PRSDKInspectorUtility.GetSectionName(property);
            if (!MatchesSearch(property, sectionName))
                continue;

            string source = PRSDKSettingsSources.GetSource(property);
            SectionGroup group = groups.Find(item => item.Source == source);

            if (group == null)
            {
                group = new SectionGroup { Source = source };
                groups.Add(group);
            }

            group.Sections.Add((property, sectionName, PRSDKInspectorUtility.GetSectionIcon(sectionType)));
        }

        groups.Sort((left, right) => PRSDKSettingsSources.Compare(left.Source, right.Source));

        return groups;
    }

    /// <summary>
    /// Рисует один раздел: заголовок с кнопкой сброса, описание и поля.
    /// </summary>
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
                EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);

                if (DrawResetButton(property, sectionName, settings, typeof(PRSDKSettings)))
                    return true;
            }

            PRSDKInspectorUtility.DrawSectionDescription(sectionType);
            EditorGUILayout.Space(2f);

            // В широком окне подпись в 150 точек обрезает длинные названия полей,
            // а значение растягивается на весь экран.
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(
                (position.width - PRSDKSidebarGUI.Width) * 0.3f,
                180f,
                340f);

            try
            {
                if (DrawChildren(property, sectionType, PRSDKInspectorUtility.GetFieldValue(settings, property)))
                    return true;
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
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
    private static bool HasExternalEditor(Type sectionType)
    {
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
            // Поиск стоит над списком, который он отбирает; сворачивать здесь нечего.
            search = GUILayout.TextField(
                search,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(PRSDKSidebarGUI.Width - 8f));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                serializedSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            var assetButton = new GUIContent("Asset", AssetDatabase.GetAssetPath(settings));
            if (GUILayout.Button(assetButton, EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }
    }

    /// <remarks>
    /// Сверяется с текущими настройками на каждой отрисовке: активный проект могли сменить.
    /// </remarks>
    private bool EnsureSettings()
    {
        if (settings != null && serializedSettings != null && settings == PRSDKSettings.Instance)
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
