using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Возвращает переводы из таблицы обратно в проект.
/// </summary>
/// <remarks>
/// Значение кладётся туда, откуда его взяли: адрес строки указывает ассет, объект внутри
/// него и сериализованное свойство. Ключ для этого не нужен — у предметов он вычисляемый
/// и в данных не хранится.
/// </remarks>
public static class LocalizationTableWriter
{
    private const string KeyField = "Key";
    private const string ValueField = "Value";

    /// <summary>
    /// Итог применения таблицы.
    /// </summary>
    public sealed class Report
    {
        /// <summary>
        /// Сколько значений изменилось.
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// Сколько языков добавлено там, где их не было.
        /// </summary>
        public int Added { get; set; }

        /// <summary>
        /// Сколько строк не нашли своё место.
        /// </summary>
        public int Missing { get; set; }

        /// <summary>
        /// Адреса, которые не нашлись.
        /// </summary>
        public List<string> MissingAddresses { get; } = new();

        /// <summary>
        /// Итог одной строкой.
        /// </summary>
        public override string ToString()
        {
            return $"Изменено значений: {Updated}, добавлено языков: {Added}, не найдено строк: {Missing}.";
        }
    }

    /// <summary>
    /// Записывает значения таблицы в проект.
    /// </summary>
    public static Report Apply(IReadOnlyList<LocalizationTableEntry> entries)
    {
        var report = new Report();

        // Правки группируются по объекту: у одного префаба переводов бывает десяток,
        // а сохранять ассет разумно один раз.
        IEnumerable<IGrouping<string, LocalizationTableEntry>> groups = entries
            .GroupBy(entry => $"{entry.AssetPath}|{entry.ObjectPath}");

        foreach (IGrouping<string, LocalizationTableEntry> group in groups)
        {
            LocalizationTableEntry first = group.First();
            UnityEngine.Object target = Resolve(first.AssetPath, first.ObjectPath);

            if (target == null)
            {
                foreach (LocalizationTableEntry entry in group)
                {
                    report.Missing++;
                    report.MissingAddresses.Add(entry.Address);
                }

                continue;
            }

            var serialized = new SerializedObject(target);
            bool changed = false;

            foreach (LocalizationTableEntry entry in group)
            {
                SerializedProperty list = serialized.FindProperty(entry.PropertyPath);

                if (list == null || !list.isArray)
                {
                    report.Missing++;
                    report.MissingAddresses.Add(entry.Address);
                    continue;
                }

                changed |= ApplyValues(list, entry, report);
                changed |= ApplyGroup(serialized, entry);
            }

            if (!changed)
                continue;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        AssetDatabase.SaveAssets();
        return report;
    }

    /// <summary>
    /// Возвращает группу записи.
    /// </summary>
    /// <remarks>
    /// Пишется там, где поле есть, — у записей общего списка и подписей на префабах.
    /// У предметов словарь лежит без обёртки: колонка для них пустая, и записывать нечего.
    /// </remarks>
    private static bool ApplyGroup(SerializedObject serialized, LocalizationTableEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Group))
            return false;

        string path = GetOwnerPath(entry.PropertyPath);

        if (path == null)
            return false;

        string backing = $"<{nameof(LocalizationControl.Group)}>k__BackingField";
        SerializedProperty group = serialized.FindProperty($"{path}.{backing}")
                                   ?? serialized.FindProperty($"{path}.{nameof(LocalizationControl.Group)}");

        if (group == null || group.propertyType != SerializedPropertyType.String)
            return false;

        if (group.stringValue == entry.Group)
            return false;

        group.stringValue = entry.Group;
        return true;
    }

    /// <summary>
    /// Путь к записи, которой принадлежит словарь.
    /// </summary>
    private static string GetOwnerPath(string propertyPath)
    {
        int cut = propertyPath.LastIndexOf("._serializedList", StringComparison.Ordinal);

        if (cut < 0)
            return null;

        string ownerPath = propertyPath.Substring(0, cut);
        int parentCut = ownerPath.LastIndexOf('.');

        return parentCut < 0 ? null : ownerPath.Substring(0, parentCut);
    }

    /// <summary>
    /// Кладёт значения одной строки в словарь.
    /// </summary>
    private static bool ApplyValues(SerializedProperty list, LocalizationTableEntry entry, Report report)
    {
        bool changed = false;
        string[] languages = Enum.GetNames(typeof(LangType));

        foreach (KeyValuePair<LangType, string> pair in entry.Values)
        {
            int languageIndex = Array.IndexOf(languages, pair.Key.ToString());

            if (languageIndex < 0)
                continue;

            SerializedProperty value = FindValue(list, languageIndex);

            if (value == null)
            {
                // Пустой перевод не создаёт запись: язык, который не переводили,
                // не должен появляться в данных пустой строкой.
                if (string.IsNullOrEmpty(pair.Value))
                    continue;

                value = AddLanguage(list, languageIndex);
                report.Added++;
                changed = true;
            }

            if (value.stringValue == pair.Value)
                continue;

            value.stringValue = pair.Value;
            report.Updated++;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Ищет значение нужного языка.
    /// </summary>
    private static SerializedProperty FindValue(SerializedProperty list, int languageIndex)
    {
        for (int index = 0; index < list.arraySize; index++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative(KeyField);

            if (key != null && key.enumValueIndex == languageIndex)
                return element.FindPropertyRelative(ValueField);
        }

        return null;
    }

    /// <summary>
    /// Заводит язык, которого в словаре ещё нет.
    /// </summary>
    private static SerializedProperty AddLanguage(SerializedProperty list, int languageIndex)
    {
        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);

        SerializedProperty element = list.GetArrayElementAtIndex(index);
        SerializedProperty key = element.FindPropertyRelative(KeyField);
        SerializedProperty value = element.FindPropertyRelative(ValueField);

        if (key != null)
            key.enumValueIndex = languageIndex;

        if (value != null)
            value.stringValue = string.Empty;

        return value;
    }

    /// <summary>
    /// Находит объект по адресу строки.
    /// </summary>
    /// <remarks>
    /// Пустой путь объекта означает ассет целиком; иначе это компонент на префабе,
    /// и после пути идёт его номер на узле.
    /// </remarks>
    private static UnityEngine.Object Resolve(string assetPath, string objectPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        if (string.IsNullOrEmpty(objectPath))
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (prefab == null)
            return null;

        int separator = objectPath.LastIndexOf('#');

        if (separator < 0 || !int.TryParse(objectPath.Substring(separator + 1), out int componentIndex))
            return null;

        string hierarchy = objectPath.Substring(0, separator);
        Transform node = string.IsNullOrEmpty(hierarchy)
            ? prefab.transform
            : prefab.transform.Find(hierarchy);

        if (node == null)
            return null;

        Component[] components = node.GetComponents<Component>();
        return componentIndex >= 0 && componentIndex < components.Length ? components[componentIndex] : null;
    }
}
