using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отдельное окно <see cref="PRSDKDatabase"/> с управлением каталогами definitions.
/// </summary>
public sealed class PRSDKDatabaseEditor : EditorWindow
{
    private readonly Dictionary<Type, UnityEngine.Object[]> assetCache = new();
    [SerializeField] private PRSDKDatabase database;
    private string search = string.Empty;
    private SerializedObject serializedDatabase;
    private Vector2 scrollPosition;

    [MenuItem("PRUnitySDK/Windows/Database", false, 10)]
    public static void OpenWindow()
    {
        PRSDKDatabaseEditor window = GetWindow<PRSDKDatabaseEditor>();
        window.titleContent = new GUIContent("SDK Database");
        window.minSize = new Vector2(620f, 450f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("SDK Database");
        minSize = new Vector2(620f, 450f);
        BindDatabase();
    }

    private void OnGUI()
    {
        if (!EnsureDatabase())
        {
            EditorGUILayout.HelpBox("Не найден asset PRSDKDatabase.", MessageType.Error);
            return;
        }

        serializedDatabase.UpdateIfRequiredOrScript();
        PRSDKInspectorUtility.DrawHeader("PRUnitySDK Database", database);
        DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        IReadOnlyList<SerializedProperty> properties =
            PRSDKInspectorUtility.GetRootProperties(serializedDatabase);
        int visibleSectionCount = 0;

        foreach (SerializedProperty property in properties)
        {
            string sectionName = PRSDKInspectorUtility.GetSectionName(property);
            if (!PRSDKInspectorUtility.MatchesSearch(sectionName, search))
                continue;

            visibleSectionCount++;
            DrawSection(property, sectionName);
        }

        if (visibleSectionCount == 0)
            EditorGUILayout.HelpBox("Секции с таким названием не найдены.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        serializedDatabase.ApplyModifiedProperties();
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
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                assetCache.Clear();
                Repaint();
            }
            if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                serializedDatabase.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("Asset", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
        }
    }

    private void DrawSection(SerializedProperty property, string sectionName)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Type fieldType = PRSDKInspectorUtility.GetFieldType(database.GetType(), property);
            Type elementType = PRSDKInspectorUtility.GetDatabaseElementType(fieldType);
            SerializedProperty data = elementType != null ? property.FindPropertyRelative("data") : null;
            bool supportsAssetTools =
                elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);
            string count = data is { isArray: true } ? $"  ({data.arraySize})" : string.Empty;

            EditorGUILayout.PropertyField(
                property,
                new GUIContent(sectionName + count),
                includeChildren: true);

            if (property.isExpanded && data is { isArray: true } && supportsAssetTools)
                DrawDatabaseTools(data, elementType);
        }

        EditorGUILayout.Space(2f);
    }

    private void DrawDatabaseTools(SerializedProperty data, Type elementType)
    {
        UnityEngine.Object[] availableAssets = GetAvailableAssets(elementType);
        int missingCount = CountMissingAssets(data, availableAssets);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(missingCount == 0))
            {
                if (GUILayout.Button($"Добавить все ({missingCount})"))
                    AddAll(data, availableAssets, elementType);
            }

            using (new EditorGUI.DisabledScope(!HasNullReferences(data)))
            {
                if (GUILayout.Button("Убрать null"))
                    RemoveNullReferences(data);
            }

            using (new EditorGUI.DisabledScope(data.arraySize == 0))
            {
                if (GUILayout.Button("Очистить"))
                    Clear(data, elementType);
            }
        }

        DrawValidation(data, elementType, availableAssets.Length);
    }

    private UnityEngine.Object[] GetAvailableAssets(Type elementType)
    {
        if (assetCache.TryGetValue(elementType, out UnityEngine.Object[] cached))
            return cached;

        string[] guids = AssetDatabase.FindAssets($"t:{elementType.Name}", new[] { "Assets" });
        UnityEngine.Object[] assets = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(AssetDatabase.LoadMainAssetAtPath)
            .Where(asset => asset != null && elementType.IsInstanceOfType(asset))
            .Distinct()
            .ToArray();

        assetCache[elementType] = assets;
        return assets;
    }

    private void AddAll(
        SerializedProperty data,
        IReadOnlyCollection<UnityEngine.Object> availableAssets,
        Type elementType)
    {
        var existing = new HashSet<UnityEngine.Object>();
        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object item = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (item != null)
                existing.Add(item);
        }

        Undo.RecordObject(database, $"Add all {elementType.Name} assets");
        int addedCount = 0;
        foreach (UnityEngine.Object asset in availableAssets)
        {
            if (!existing.Add(asset))
                continue;

            int index = data.arraySize;
            data.InsertArrayElementAtIndex(index);
            data.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            addedCount++;
        }

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        Debug.Log($"[PRSDKDatabase] Добавлено {addedCount} assets типа {elementType.Name}.", database);
        GUIUtility.ExitGUI();
    }

    private void RemoveNullReferences(SerializedProperty data)
    {
        Undo.RecordObject(database, "Remove null database entries");
        for (int index = data.arraySize - 1; index >= 0; index--)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue != null)
                continue;

            DeleteArrayElement(data, index);
        }

        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private void Clear(SerializedProperty data, Type elementType)
    {
        if (!EditorUtility.DisplayDialog(
                "Очистить базу?",
                $"Все ссылки типа {elementType.Name} будут удалены из этой секции. Сами assets останутся в проекте.",
                "Очистить",
                "Отмена"))
        {
            return;
        }

        Undo.RecordObject(database, $"Clear {elementType.Name} database");
        data.ClearArray();
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        GUIUtility.ExitGUI();
    }

    private static void DrawValidation(
        SerializedProperty data,
        Type elementType,
        int availableAssetCount)
    {
        DatabaseValidation validation = Validate(data);
        if (!validation.HasIssues)
        {
            EditorGUILayout.HelpBox(
                $"В списке: {data.arraySize}. Найдено в проекте: {availableAssetCount}. " +
                $"Повторяющихся ссылок и Id нет.",
                MessageType.Info);
            return;
        }

        var messages = new List<string>();
        if (validation.NullCount > 0)
            messages.Add($"пустых ссылок: {validation.NullCount}");
        if (validation.DuplicateReferenceCount > 0)
            messages.Add($"повторяющихся assets: {validation.DuplicateReferenceCount}");
        if (validation.EmptyIdCount > 0)
            messages.Add($"пустых Id: {validation.EmptyIdCount}");
        if (validation.DuplicateIdCount > 0)
            messages.Add($"повторяющихся Id: {validation.DuplicateIdCount}");

        EditorGUILayout.HelpBox(
            $"Проблемы {elementType.Name}: {string.Join(", ", messages)}.",
            MessageType.Warning);
    }

    private static DatabaseValidation Validate(SerializedProperty data)
    {
        var references = new HashSet<UnityEngine.Object>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var validation = new DatabaseValidation();

        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object item = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (item == null)
            {
                validation.NullCount++;
                continue;
            }

            if (!references.Add(item))
                validation.DuplicateReferenceCount++;

            if (item is not IIdentifiable identifiable)
                continue;

            if (string.IsNullOrWhiteSpace(identifiable.Id))
                validation.EmptyIdCount++;
            else if (!ids.Add(identifiable.Id))
                validation.DuplicateIdCount++;
        }

        return validation;
    }

    private static int CountMissingAssets(
        SerializedProperty data,
        IEnumerable<UnityEngine.Object> availableAssets)
    {
        var existing = new HashSet<UnityEngine.Object>();
        for (int index = 0; index < data.arraySize; index++)
        {
            UnityEngine.Object item = data.GetArrayElementAtIndex(index).objectReferenceValue;
            if (item != null)
                existing.Add(item);
        }

        return availableAssets.Count(asset => !existing.Contains(asset));
    }

    private static bool HasNullReferences(SerializedProperty data)
    {
        for (int index = 0; index < data.arraySize; index++)
        {
            if (data.GetArrayElementAtIndex(index).objectReferenceValue == null)
                return true;
        }

        return false;
    }

    private static void DeleteArrayElement(SerializedProperty array, int index)
    {
        int oldSize = array.arraySize;
        array.DeleteArrayElementAtIndex(index);
        if (array.arraySize == oldSize)
            array.DeleteArrayElementAtIndex(index);
    }

    private void SetExpanded(bool expanded)
    {
        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serializedDatabase))
            property.isExpanded = expanded;

        Repaint();
    }

    private bool EnsureDatabase()
    {
        if (database != null && serializedDatabase != null)
            return true;

        BindDatabase();
        return database != null && serializedDatabase != null;
    }

    private void BindDatabase()
    {
        database = PRSDKDatabase.Instance;
        serializedDatabase = database != null ? new SerializedObject(database) : null;
        assetCache.Clear();
    }

    private sealed class DatabaseValidation
    {
        public int NullCount;
        public int DuplicateReferenceCount;
        public int EmptyIdCount;
        public int DuplicateIdCount;

        public bool HasIssues =>
            NullCount > 0 ||
            DuplicateReferenceCount > 0 ||
            EmptyIdCount > 0 ||
            DuplicateIdCount > 0;
    }
}
