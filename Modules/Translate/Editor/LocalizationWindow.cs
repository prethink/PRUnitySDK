using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public partial class LocalizationWindow : ExtendedEditorWindow
{
    private static readonly Regex PlaceholderRegex = new(
        @"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})",
        RegexOptions.Compiled);

    /// <summary>
    /// Длина, после которой перевод перестаёт помещаться в одну строку.
    /// </summary>
    private const int SingleLineLength = 90;

    private SerializedObject serializedDatabase;
    private SerializedProperty defaultLanguageProperty;
    private SerializedProperty commonProperty;
    private SerializedProperty projectProperty;
    private Vector2 commonScroll;
    private Vector2 projectScroll;
    private string search = string.Empty;
    private LangType[] languages = Array.Empty<LangType>();

    [MenuItem("PRUnitySDK/Tools/Localization")]
    public static void Open()
    {
        GetWindow<LocalizationWindow>("Localization");
    }

    private void OnEnable()
    {
        Initialize();
        InitializeTable();
    }

    private void OnDisable()
    {
        SaveTableSettings();
    }

    private void Initialize()
    {
        database = ScriptableObjectSingleton<PRSDKDatabase>.Instance;
        languages = Enum.GetValues(typeof(LangType)).Cast<LangType>().ToArray();

        if (database == null)
        {
            serializedDatabase = null;
            return;
        }

        serializedDatabase = new SerializedObject(database);
        SerializedProperty localization = serializedDatabase.FindProperty(
            nameof(PRUnitySDK.Database.LocalizationDatabase).GetBackingField());

        defaultLanguageProperty = localization?.FindPropertyRelative(
            nameof(LocalizationDatabase.DefaultLanguage).GetBackingField());
        commonProperty = localization?.FindPropertyRelative(
            nameof(LocalizationDatabase.Common).GetBackingField());
        projectProperty = localization?.FindPropertyRelative(
            nameof(LocalizationDatabase.Project).GetBackingField());
    }

    private void OnGUI()
    {
        DrawSearch();

        // Вкладка проекта работает и без базы: переводы предметов и подписи на префабах
        // лежат в своих ассетах, и отсутствие базы им не мешает.
        Tabs(
            ("Common", () => DrawDatabaseTab(commonProperty, projectProperty, ref commonScroll, "Common")),
            ("Project", () => DrawDatabaseTab(projectProperty, commonProperty, ref projectScroll, "Project")),
            ("Проект", DrawTableTab));
    }

    /// <summary>
    /// Рисует вкладку общего списка.
    /// </summary>
    private void DrawDatabaseTab(
        SerializedProperty list,
        SerializedProperty other,
        ref Vector2 scroll,
        string listName)
    {
        if (database == null || serializedDatabase == null ||
            commonProperty == null || projectProperty == null)
        {
            EditorGUILayout.HelpBox("Localization database was not found.", MessageType.Error);

            if (GUILayout.Button("Retry"))
                Initialize();

            return;
        }

        serializedDatabase.Update();
        DrawToolbar();
        DrawList(list, other, ref scroll, listName);
        serializedDatabase.ApplyModifiedProperties();
    }

    /// <summary>
    /// Поле поиска, общее для всех вкладок.
    /// </summary>
    private void DrawSearch()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));

        if (!string.IsNullOrEmpty(search) &&
            GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22f)))
            search = string.Empty;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Default", GUILayout.Width(45f));
        EditorGUILayout.PropertyField(defaultLanguageProperty, GUIContent.none, GUILayout.Width(105f));

        if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(65f)))
            ShowValidationResult();

        if (GUILayout.Button("Fix Languages", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            AddMissingLanguagesToAll();

        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48f)))
        {
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawList(
        SerializedProperty list,
        SerializedProperty otherList,
        ref Vector2 scroll,
        string listName)
    {
        Dictionary<string, int> keyCounts = GetKeyCounts(list);
        HashSet<string> otherKeys = GetKeys(otherList);
        int visibleCount = CountVisible(list);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            $"{listName}: {visibleCount}/{list.arraySize}",
            EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(!string.IsNullOrWhiteSpace(search)))
        {
            if (GUILayout.Button("Add Key", GUILayout.Width(70f)))
                AddEntry(list);
        }

        using (new EditorGUI.DisabledScope(list.arraySize == 0))
        {
            if (GUILayout.Button("Clear All", GUILayout.Width(70f)))
                ClearList(list, listName);
        }

        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        for (int index = 0; index < list.arraySize; index++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            if (!MatchesSearch(element))
                continue;

            DrawEntry(list, index, element, keyCounts, otherKeys, listName);
        }

        if (visibleCount == 0)
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(search)
                    ? "No localization keys configured."
                    : "No localization keys match the current search.",
                MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        SerializedProperty list,
        int index,
        SerializedProperty element,
        IReadOnlyDictionary<string, int> keyCounts,
        ISet<string> otherKeys,
        string listName)
    {
        SerializedProperty key = GetKeyProperty(element);
        SerializedProperty dictionary = GetDictionaryList(element);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"#{index + 1}", EditorStyles.boldLabel, GUILayout.Width(34f));
        EditorGUILayout.PropertyField(key, GUIContent.none);

        if (GUILayout.Button("Duplicate", GUILayout.Width(72f)))
            DuplicateEntry(list, index);

        if (GUILayout.Button("Delete", GUILayout.Width(54f)) &&
            EditorUtility.DisplayDialog(
                "Delete Localization Key",
                $"Delete '{key.stringValue}' from {listName}?",
                "Delete",
                "Cancel"))
        {
            Undo.RecordObject(database, "Delete Localization Key");
            list.DeleteArrayElementAtIndex(index);
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        DrawTranslations(dictionary);
        DrawValidation(key.stringValue, dictionary, keyCounts, otherKeys, listName);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(1f);
    }

    private void DrawTranslations(SerializedProperty dictionary)
    {
        if (dictionary == null)
        {
            EditorGUILayout.HelpBox("Localization dictionary is missing.", MessageType.Error);
            return;
        }

        foreach (LangType language in languages)
        {
            SerializedProperty pair = FindPair(dictionary, language);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                ObjectNames.NicifyVariableName(language.ToString()),
                EditorStyles.miniLabel,
                GUILayout.Width(64f));

            if (pair == null)
            {
                EditorGUILayout.LabelField("Missing", EditorStyles.miniLabel);
                if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(45f)))
                    AddLanguage(dictionary, language);
            }
            else
            {
                DrawTranslationValue(pair.FindPropertyRelative("Value"));
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Поле перевода высотой по содержимому.
    /// </summary>
    /// <remarks>
    /// Подписей интерфейса подавляющее большинство, и все они в одну строку: постоянная
    /// многострочная область отнимала бы у списка вдвое больше места, чем показывает.
    /// Поле растёт, только когда в тексте действительно есть что показывать — перенос
    /// строки или длинное описание.
    /// </remarks>
    private static void DrawTranslationValue(SerializedProperty value)
    {
        string text = value.stringValue ?? string.Empty;
        int breaks = text.Count(symbol => symbol == '\n');

        if (breaks == 0 && text.Length <= SingleLineLength)
        {
            value.stringValue = EditorGUILayout.TextField(text);
            return;
        }

        int lines = Mathf.Clamp(breaks + 1 + text.Length / SingleLineLength, 2, 6);

        value.stringValue = EditorGUILayout.TextArea(
            text,
            GUILayout.Height(lines * EditorGUIUtility.singleLineHeight));
    }

    private void DrawValidation(
        string key,
        SerializedProperty dictionary,
        IReadOnlyDictionary<string, int> keyCounts,
        ISet<string> otherKeys,
        string listName)
    {
        if (string.IsNullOrWhiteSpace(key))
            EditorGUILayout.HelpBox("Localization key is required.", MessageType.Error);
        else
        {
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                EditorGUILayout.HelpBox("Key contains leading or trailing whitespace.", MessageType.Error);

            if (keyCounts.TryGetValue(key.Trim(), out int count) && count > 1)
                EditorGUILayout.HelpBox("Key is duplicated in this list.", MessageType.Error);

            if (otherKeys.Contains(key.Trim()))
            {
                EditorGUILayout.HelpBox(
                    listName == "Project"
                        ? "This Project key overrides a Common key."
                        : "This Common key is overridden by a Project key.",
                    MessageType.Info);
            }
        }

        if (dictionary == null)
            return;

        var seenLanguages = new HashSet<LangType>();
        for (int index = 0; index < dictionary.arraySize; index++)
        {
            SerializedProperty pair = dictionary.GetArrayElementAtIndex(index);
            LangType language = (LangType)pair.FindPropertyRelative("Key").enumValueIndex;
            if (!seenLanguages.Add(language))
                EditorGUILayout.HelpBox($"Language '{language}' is duplicated.", MessageType.Error);
        }

        foreach (LangType language in languages)
        {
            SerializedProperty pair = FindPair(dictionary, language);
            if (pair == null)
            {
                EditorGUILayout.HelpBox($"Translation for '{language}' is missing.", MessageType.Warning);
                continue;
            }

            string value = pair.FindPropertyRelative("Value").stringValue;
            if (string.IsNullOrWhiteSpace(value))
                EditorGUILayout.HelpBox($"Translation for '{language}' is empty.", MessageType.Warning);
        }

        ValidatePlaceholders(dictionary);
    }

    private void ValidatePlaceholders(SerializedProperty dictionary)
    {
        LangType defaultLanguage = (LangType)defaultLanguageProperty.enumValueIndex;
        SerializedProperty defaultPair = FindPair(dictionary, defaultLanguage);
        if (defaultPair == null)
            return;

        string expected = GetPlaceholderSignature(
            defaultPair.FindPropertyRelative("Value").stringValue);

        foreach (LangType language in languages)
        {
            SerializedProperty pair = FindPair(dictionary, language);
            if (pair == null)
                continue;

            string actual = GetPlaceholderSignature(pair.FindPropertyRelative("Value").stringValue);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox(
                    $"Placeholders in '{language}' do not match the default language " +
                    $"({FormatSignature(expected)} expected, {FormatSignature(actual)} found).",
                    MessageType.Error);
            }
        }
    }

    private void AddEntry(SerializedProperty list)
    {
        Undo.RecordObject(database, "Add Localization Key");
        serializedDatabase.Update();

        int index = list.arraySize;
        list.arraySize++;
        SerializedProperty element = list.GetArrayElementAtIndex(index);
        GetKeyProperty(element).stringValue = GetUniqueKey(list, "new_key", index);

        SerializedProperty dictionary = GetDictionaryList(element);
        dictionary?.ClearArray();
        if (dictionary != null)
        {
            foreach (LangType language in languages)
                AddLanguage(dictionary, language);
        }

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private void DuplicateEntry(SerializedProperty list, int index)
    {
        Undo.RecordObject(database, "Duplicate Localization Key");
        list.InsertArrayElementAtIndex(index);
        SerializedProperty copy = list.GetArrayElementAtIndex(index + 1);
        SerializedProperty key = GetKeyProperty(copy);
        key.stringValue = GetUniqueKey(list, key.stringValue + "_copy", index + 1);
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private void ClearList(SerializedProperty list, string listName)
    {
        if (!EditorUtility.DisplayDialog(
                $"Clear {listName} Localization",
                $"Delete all {list.arraySize} keys from {listName}?",
                "Clear All",
                "Cancel"))
            return;

        Undo.RecordObject(database, $"Clear {listName} Localization");
        list.ClearArray();
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private void AddMissingLanguagesToAll()
    {
        Undo.RecordObject(database, "Fix Localization Languages");
        serializedDatabase.Update();

        int added = AddMissingLanguages(commonProperty) + AddMissingLanguages(projectProperty);
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        EditorUtility.DisplayDialog("Localization", $"Added {added} missing language entries.", "OK");
    }

    private int AddMissingLanguages(SerializedProperty list)
    {
        int added = 0;
        for (int index = 0; index < list.arraySize; index++)
        {
            SerializedProperty dictionary = GetDictionaryList(list.GetArrayElementAtIndex(index));
            if (dictionary == null)
                continue;

            foreach (LangType language in languages)
            {
                if (FindPair(dictionary, language) != null)
                    continue;

                AddLanguage(dictionary, language);
                added++;
            }
        }

        return added;
    }

    private void ShowValidationResult()
    {
        int errors = 0;
        int warnings = 0;
        ValidateList(commonProperty, ref errors, ref warnings);
        ValidateList(projectProperty, ref errors, ref warnings);

        EditorUtility.DisplayDialog(
            "Localization Validation",
            errors == 0 && warnings == 0
                ? $"All {commonProperty.arraySize + projectProperty.arraySize} keys are valid."
                : $"Errors: {errors}\nWarnings: {warnings}",
            "OK");
    }

    private void ValidateList(SerializedProperty list, ref int errors, ref int warnings)
    {
        Dictionary<string, int> keyCounts = GetKeyCounts(list);
        LangType defaultLanguage = (LangType)defaultLanguageProperty.enumValueIndex;

        for (int index = 0; index < list.arraySize; index++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            string key = GetKeyProperty(element).stringValue;
            if (string.IsNullOrWhiteSpace(key) || key != key.Trim())
                errors++;
            else if (keyCounts.TryGetValue(key, out int count) && count > 1)
                errors++;

            SerializedProperty dictionary = GetDictionaryList(element);
            if (dictionary == null)
            {
                errors++;
                continue;
            }

            var seen = new HashSet<LangType>();
            for (int pairIndex = 0; pairIndex < dictionary.arraySize; pairIndex++)
            {
                LangType language = (LangType)dictionary.GetArrayElementAtIndex(pairIndex)
                    .FindPropertyRelative("Key").enumValueIndex;
                if (!seen.Add(language))
                    errors++;
            }

            SerializedProperty defaultPair = FindPair(dictionary, defaultLanguage);
            string expected = defaultPair == null
                ? string.Empty
                : GetPlaceholderSignature(defaultPair.FindPropertyRelative("Value").stringValue);

            foreach (LangType language in languages)
            {
                SerializedProperty pair = FindPair(dictionary, language);
                if (pair == null || string.IsNullOrWhiteSpace(pair.FindPropertyRelative("Value").stringValue))
                {
                    warnings++;
                    continue;
                }

                string actual = GetPlaceholderSignature(pair.FindPropertyRelative("Value").stringValue);
                if (defaultPair != null && actual != expected)
                    errors++;
            }
        }
    }

    private bool MatchesSearch(SerializedProperty element)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        string query = search.Trim();
        if (Contains(GetKeyProperty(element)?.stringValue, query))
            return true;

        SerializedProperty dictionary = GetDictionaryList(element);
        if (dictionary == null)
            return false;

        for (int index = 0; index < dictionary.arraySize; index++)
        {
            string value = dictionary.GetArrayElementAtIndex(index)
                .FindPropertyRelative("Value").stringValue;
            if (Contains(value, query))
                return true;
        }

        return false;
    }

    private int CountVisible(SerializedProperty list)
    {
        int count = 0;
        for (int index = 0; index < list.arraySize; index++)
        {
            if (MatchesSearch(list.GetArrayElementAtIndex(index)))
                count++;
        }

        return count;
    }

    private static Dictionary<string, int> GetKeyCounts(SerializedProperty list)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < list.arraySize; index++)
        {
            string key = GetKeyProperty(list.GetArrayElementAtIndex(index)).stringValue?.Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            result.TryGetValue(key, out int count);
            result[key] = count + 1;
        }

        return result;
    }

    private static HashSet<string> GetKeys(SerializedProperty list)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < list.arraySize; index++)
        {
            string key = GetKeyProperty(list.GetArrayElementAtIndex(index)).stringValue?.Trim();
            if (!string.IsNullOrEmpty(key))
                result.Add(key);
        }

        return result;
    }

    private static string GetUniqueKey(SerializedProperty list, string baseKey, int ignoredIndex)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < list.arraySize; index++)
        {
            if (index == ignoredIndex)
                continue;

            keys.Add(GetKeyProperty(list.GetArrayElementAtIndex(index)).stringValue ?? string.Empty);
        }

        string candidate = baseKey;
        int suffix = 2;
        while (keys.Contains(candidate))
            candidate = $"{baseKey}_{suffix++}";

        return candidate;
    }

    private static void AddLanguage(SerializedProperty dictionary, LangType language)
    {
        int index = dictionary.arraySize;
        dictionary.arraySize++;
        SerializedProperty pair = dictionary.GetArrayElementAtIndex(index);
        pair.FindPropertyRelative("Key").enumValueIndex = (int)language;
        pair.FindPropertyRelative("Value").stringValue = string.Empty;
    }

    private static SerializedProperty FindPair(SerializedProperty dictionary, LangType language)
    {
        if (dictionary == null)
            return null;

        for (int index = 0; index < dictionary.arraySize; index++)
        {
            SerializedProperty pair = dictionary.GetArrayElementAtIndex(index);
            if ((LangType)pair.FindPropertyRelative("Key").enumValueIndex == language)
                return pair;
        }

        return null;
    }

    private static SerializedProperty GetKeyProperty(SerializedProperty element)
    {
        return element?.FindPropertyRelative(
            nameof(LocalizationControl.LocalizationKey).GetBackingField());
    }

    private static SerializedProperty GetDictionaryList(SerializedProperty element)
    {
        return element?
            .FindPropertyRelative(LocalizationControl.InternalLocalizationValuesPropertyName.GetBackingField())?
            .FindPropertyRelative("_serializedList");
    }

    private static string GetPlaceholderSignature(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return string.Join(",", PlaceholderRegex.Matches(value)
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .OrderBy(index => index, StringComparer.Ordinal));
    }

    private static string FormatSignature(string signature)
    {
        return string.IsNullOrEmpty(signature) ? "none" : $"{{{signature}}}";
    }

    /// <summary>
    /// Значение содержит искомое, без учёта регистра.
    /// </summary>
    private static bool Contains(string value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
