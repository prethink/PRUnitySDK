using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Находит переводы по всему проекту.
/// </summary>
/// <remarks>
/// Переводы живут не в одном месте: общий список лежит в базе SDK, у предметов и наград
/// свои словари в ассетах, а подписи интерфейса — прямо на префабах окон. Собирать их
/// глазами при добавлении языка не вариант, поэтому сборщик ищет сам.
/// <para>
/// Ищется не по именам полей, а по форме данных: словарь, ключами которого служат языки,
/// а значениями — строки. Так находится и новый тип, о котором сборщик ничего не знает.
/// </para>
/// </remarks>
public static class LocalizationTableCollector
{
    /// <summary>
    /// Имя внутреннего списка у сериализуемого словаря.
    /// </summary>
    private const string SerializedListName = "_serializedList";

    private const string KeyField = "Key";
    private const string ValueField = "Value";

    /// <summary>
    /// Собирает переводы из указанных папок.
    /// </summary>
    /// <param name="searchFolders">Папки проекта; пусто — весь проект.</param>
    /// <param name="includePrefabs">Заглядывать ли в префабы.</param>
    public static List<LocalizationTableEntry> Collect(string[] searchFolders, bool includePrefabs = true)
    {
        var entries = new List<LocalizationTableEntry>();
        string[] folders = searchFolders != null && searchFolders.Length > 0 ? searchFolders : null;

        CollectFromAssets("t:ScriptableObject", folders, entries, LocalizationTableSource.Asset);

        if (includePrefabs)
            CollectFromPrefabs(folders, entries);

        MarkDatabaseEntries(entries);
        return entries;
    }

    /// <summary>
    /// Обходит ассеты указанного типа.
    /// </summary>
    private static void CollectFromAssets(
        string filter,
        string[] folders,
        List<LocalizationTableEntry> entries,
        LocalizationTableSource source)
    {
        foreach (string guid in FindAssets(filter, folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset == null)
                continue;

            CollectFromObject(asset, path, string.Empty, source, entries);
        }
    }

    /// <summary>
    /// Обходит префабы и все компоненты внутри них.
    /// </summary>
    /// <remarks>
    /// Компонент адресуется путём объекта и его номером на этом объекте: двух одинаковых
    /// компонентов на одном узле хватает, чтобы путь перестал быть уникальным.
    /// </remarks>
    private static void CollectFromPrefabs(string[] folders, List<LocalizationTableEntry> entries)
    {
        foreach (string guid in FindAssets("t:Prefab", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                Component[] components = child.GetComponents<Component>();

                for (int index = 0; index < components.Length; index++)
                {
                    Component component = components[index];

                    if (component == null)
                        continue;

                    string objectPath = $"{GetHierarchyPath(prefab.transform, child)}#{index}";
                    CollectFromObject(component, path, objectPath, LocalizationTableSource.Prefab, entries);
                }
            }
        }
    }

    /// <summary>
    /// Ищет словари переводов внутри одного объекта.
    /// </summary>
    private static void CollectFromObject(
        UnityEngine.Object target,
        string assetPath,
        string objectPath,
        LocalizationTableSource source,
        List<LocalizationTableEntry> entries)
    {
        SerializedObject serialized;

        try
        {
            serialized = new SerializedObject(target);
        }
        catch (Exception)
        {
            // Битый скрипт или объект без сериализации — просто пропускаем.
            return;
        }

        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.name != SerializedListName || !IsLanguageDictionary(property))
                continue;

            LocalizationTableEntry entry = ReadEntry(property);
            entry.AssetPath = assetPath;
            entry.ObjectPath = objectPath;
            entry.Source = source;
            entry.Owner = GetOwnerName(target, objectPath);
            entry.Target = target;
            entries.Add(entry);
        }
    }

    /// <summary>
    /// Список выглядит как словарь «язык — строка».
    /// </summary>
    /// <remarks>
    /// Пустой словарь тоже подходит: язык у него ещё не заведён, но заводить придётся
    /// именно здесь, и в таблице такая строка нужна.
    /// </remarks>
    private static bool IsLanguageDictionary(SerializedProperty list)
    {
        if (!list.isArray)
            return false;

        SerializedProperty element = list.arraySize > 0
            ? list.GetArrayElementAtIndex(0)
            : null;

        if (element == null)
            return IsLanguageDictionaryByType(list);

        SerializedProperty key = element.FindPropertyRelative(KeyField);
        SerializedProperty value = element.FindPropertyRelative(ValueField);

        return key != null
               && value != null
               && key.propertyType == SerializedPropertyType.Enum
               && value.propertyType == SerializedPropertyType.String
               && IsLanguageEnum(key);
    }

    /// <summary>
    /// Проверяет пустой словарь по объявленному типу элемента.
    /// </summary>
    private static bool IsLanguageDictionaryByType(SerializedProperty list)
    {
        string type = list.arrayElementType;
        return !string.IsNullOrEmpty(type) && type.Contains(nameof(LangType));
    }

    /// <summary>
    /// Ключами служат языки, а не какое-то другое перечисление.
    /// </summary>
    private static bool IsLanguageEnum(SerializedProperty key)
    {
        string[] languages = Enum.GetNames(typeof(LangType));
        return key.enumNames != null && key.enumNames.SequenceEqual(languages);
    }

    /// <summary>
    /// Читает значения словаря и ключ перевода, если он лежит рядом.
    /// </summary>
    private static LocalizationTableEntry ReadEntry(SerializedProperty list)
    {
        string entryKey = FindNeighbourValue(list, nameof(LocalizationControl.LocalizationKey));
        string entryGroup = FindNeighbourValue(list, nameof(LocalizationControl.Group));

        var entry = new LocalizationTableEntry
        {
            PropertyPath = list.propertyPath,
            Key = entryKey,

            // Пустая группа выводится из ключа — так же, как в списках окна: подписи
            // с общим префиксом и в таблице должны оказаться рядом.
            Group = string.IsNullOrEmpty(entryGroup)
                ? LocalizationControl.GetGroupFromKey(entryKey)
                : entryGroup
        };

        string[] languages = Enum.GetNames(typeof(LangType));

        for (int index = 0; index < list.arraySize; index++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative(KeyField);
            SerializedProperty value = element.FindPropertyRelative(ValueField);

            if (key == null || value == null)
                continue;

            if (key.enumValueIndex < 0 || key.enumValueIndex >= languages.Length)
                continue;

            var language = (LangType)Enum.Parse(typeof(LangType), languages[key.enumValueIndex]);
            entry.Values[language] = value.stringValue;
        }

        return entry;
    }

    /// <summary>
    /// Читает соседнее с словарём строковое свойство: ключ или группу.
    /// </summary>
    /// <remarks>
    /// У записей общего списка они сериализованы рядом со словарём. У предметов словарь
    /// лежит без обёртки — тогда значение пустое, а строку опознаёт адрес.
    /// </remarks>
    private static string FindNeighbourValue(SerializedProperty list, string propertyName)
    {
        SerializedObject serialized = list.serializedObject;
        string path = list.propertyPath;

        // Поднимаемся от словаря к владельцу и смотрим, нет ли у него такого поля.
        int cut = path.LastIndexOf($".{SerializedListName}", StringComparison.Ordinal);

        if (cut < 0)
            return string.Empty;

        string ownerPath = path.Substring(0, cut);
        int parentCut = ownerPath.LastIndexOf('.');
        string owner = parentCut < 0 ? string.Empty : ownerPath.Substring(0, parentCut);

        return ReadString(serialized, owner, propertyName);
    }

    /// <summary>
    /// Читает строковое свойство у владельца словаря.
    /// </summary>
    private static string ReadString(SerializedObject serialized, string ownerPath, string propertyName)
    {
        string backing = $"<{propertyName}>k__BackingField";

        string[] candidates = string.IsNullOrEmpty(ownerPath)
            ? new[] { backing, propertyName }
            : new[] { $"{ownerPath}.{backing}", $"{ownerPath}.{propertyName}" };

        foreach (string candidate in candidates)
        {
            SerializedProperty property = serialized.FindProperty(candidate);

            if (property != null && property.propertyType == SerializedPropertyType.String)
                return property.stringValue;
        }

        return string.Empty;
    }

    /// <summary>
    /// Отмечает записи общего списка базы отдельным источником.
    /// </summary>
    private static void MarkDatabaseEntries(List<LocalizationTableEntry> entries)
    {
        foreach (LocalizationTableEntry entry in entries)
        {
            if (entry.Target is PRSDKDatabase)
                entry.Source = LocalizationTableSource.Database;
        }
    }

    /// <summary>
    /// Имя, по которому переводчик поймёт, о чём строка.
    /// </summary>
    private static string GetOwnerName(UnityEngine.Object target, string objectPath)
    {
        if (target is Component component)
            return $"{component.gameObject.name} ({component.GetType().Name})";

        return string.IsNullOrEmpty(objectPath) ? target.name : $"{target.name}/{objectPath}";
    }

    /// <summary>
    /// Путь объекта внутри префаба.
    /// </summary>
    private static string GetHierarchyPath(Transform root, Transform target)
    {
        if (target == root)
            return string.Empty;

        var parts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string[] FindAssets(string filter, string[] folders)
    {
        return folders == null
            ? AssetDatabase.FindAssets(filter)
            : AssetDatabase.FindAssets(filter, folders);
    }
}
