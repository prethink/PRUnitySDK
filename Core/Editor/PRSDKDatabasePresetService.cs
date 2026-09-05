using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Сохраняет и загружает состав базы: какие определения входят в сборку игры.
/// </summary>
/// <remarks>
/// Загрузка идёт в два шага. Сначала <see cref="Analyze"/> сверяет набор с проектом
/// и возвращает отчёт, и только потом <see cref="Apply"/> меняет базу. Набор приезжает
/// из другой игры или из другой ветки, где ассеты могли переехать, переименоваться
/// или исчезнуть, поэтому применять набор вслепую нельзя.
/// </remarks>
public static class PRSDKDatabasePresetService
{
    /// <summary>
    /// Папка наборов по умолчанию.
    /// </summary>
    /// <remarks>
    /// Рядом с <c>Assets</c>, а не внутри: наборы нужны только редактору, Unity незачем
    /// импортировать их как ассеты, а git их всё равно видит.
    /// </remarks>
    public static string DefaultFolder =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "DatabasePresets");

    #region Сохранение

    /// <summary>
    /// Снимает текущий состав базы.
    /// </summary>
    /// <param name="database">База, состав которой нужно сохранить.</param>
    /// <param name="presetName">Имя набора.</param>
    /// <param name="scope">
    /// Отбор каталогов: набор описывает то, что показывает окно, а не всю базу.
    /// Пусто — берётся всё, как было до разделения базы на окна.
    /// </param>
    public static PRSDKDatabasePreset Capture(
        PRSDKDatabase database,
        string presetName,
        Func<SerializedProperty, bool> scope = null)
    {
        var preset = new PRSDKDatabasePreset
        {
            name = presetName,
            savedAt = DateTime.Now.ToString("s"),
            project = Application.productName
        };

        var serialized = new SerializedObject(database);

        foreach (SerializedProperty catalog in EnumerateCatalogs(serialized))
        {
            if (scope != null && !scope(catalog))
                continue;

            var section = new PRSDKDatabasePresetSection
            {
                path = catalog.propertyPath,
                label = BuildLabel(serialized, catalog),
                elementType = ExtractElementType(catalog)
            };

            for (int index = 0; index < catalog.arraySize; index++)
            {
                UnityEngine.Object value = catalog.GetArrayElementAtIndex(index).objectReferenceValue;

                // Пустые ссылки не сохраняем: набор описывает состав, а дырка в списке -
                // это дефект текущей базы, и переносить его в другую игру незачем.
                if (value == null)
                    continue;

                section.items.Add(Describe(value));
            }

            preset.sections.Add(section);
        }

        return preset;
    }

    /// <summary>
    /// Описывает ассет так, чтобы его можно было найти в другом проекте.
    /// </summary>
    private static PRSDKDatabasePresetItem Describe(UnityEngine.Object asset)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);

        return new PRSDKDatabasePresetItem
        {
            guid = guid,
            localId = localId,
            path = AssetDatabase.GetAssetPath(asset),
            name = asset.name,
            type = asset.GetType().Name
        };
    }

    /// <summary>
    /// Записывает набор в файл.
    /// </summary>
    public static void Save(PRSDKDatabasePreset preset, string filePath)
    {
        string folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(filePath, EditorJsonUtility.ToJson(preset, true));
    }

    /// <summary>
    /// Читает набор из файла.
    /// </summary>
    /// <returns><see langword="null"/>, если файл не читается.</returns>
    public static PRSDKDatabasePreset Load(string filePath, out string error)
    {
        error = null;

        if (!File.Exists(filePath))
        {
            error = $"Файл «{filePath}» не найден.";
            return null;
        }

        try
        {
            var preset = new PRSDKDatabasePreset();
            EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(filePath), preset);

            if (preset.sections == null)
            {
                error = "Файл не похож на набор: нет списка каталогов.";
                return null;
            }

            return preset;
        }
        catch (Exception exception)
        {
            error = $"Не удалось прочитать набор: {exception.Message}";
            return null;
        }
    }

    #endregion

    #region Проверка

    /// <summary>
    /// Сверяет набор с проектом и готовит план применения.
    /// </summary>
    /// <remarks>
    /// Ничего не меняет: отчёт нужен, чтобы решение принимал человек.
    /// </remarks>
    /// <param name="scope">
    /// Отбор каталогов окна. Разделы набора вне этого отбора не применяются: окно правит
    /// только то, что показывает, иначе из каталога предметов можно было бы переписать
    /// награды и звуки.
    /// </param>
    public static PRSDKDatabasePresetReport Analyze(
        PRSDKDatabasePreset preset,
        PRSDKDatabase database,
        Func<SerializedProperty, bool> scope = null)
    {
        var report = new PRSDKDatabasePresetReport(preset);
        var serialized = new SerializedObject(database);

        Dictionary<string, SerializedProperty> catalogs = EnumerateCatalogs(serialized)
            .Where(property => scope == null || scope(property))
            .ToDictionary(property => property.propertyPath, property => property);

        var touched = new HashSet<string>(StringComparer.Ordinal);

        foreach (PRSDKDatabasePresetSection section in preset.sections)
        {
            if (!catalogs.TryGetValue(section.path, out SerializedProperty catalog))
            {
                // Каталог либо исчез из базы, либо принадлежит другому окну. Второе —
                // обычное дело: набор мог быть снят там, где видно больше.
                report.AddIssue(
                    PRSDKDatabasePresetSeverity.Info,
                    section.label,
                    $"«{section.label}» не относится к этому окну — раздел пропущен.");
                continue;
            }

            touched.Add(section.path);
            report.Sections.Add(ResolveSection(section, catalog, report));
        }

        // Каталоги, которых в наборе нет, остаются как есть: набор может быть старше базы,
        // и вычищать раздел, которого он не знает, нельзя.
        foreach (string path in catalogs.Keys.Where(path => !touched.Contains(path)))
        {
            report.AddIssue(
                PRSDKDatabasePresetSeverity.Info,
                BuildLabel(serialized, catalogs[path]),
                "Каталога нет в наборе — останется без изменений.");
        }

        return report;
    }

    /// <summary>
    /// Разбирает один каталог набора.
    /// </summary>
    private static PRSDKDatabasePresetResolvedSection ResolveSection(
        PRSDKDatabasePresetSection section,
        SerializedProperty catalog,
        PRSDKDatabasePresetReport report)
    {
        var resolved = new PRSDKDatabasePresetResolvedSection(section.path, section.label);

        string expectedType = ExtractElementType(catalog);
        if (!string.IsNullOrEmpty(section.elementType)
            && !string.IsNullOrEmpty(expectedType)
            && section.elementType != expectedType)
        {
            report.AddIssue(
                PRSDKDatabasePresetSeverity.Error,
                section.label,
                $"Каталог теперь хранит «{expectedType}», а в наборе «{section.elementType}» — раздел пропущен.");
            resolved.Skipped = true;
            return resolved;
        }

        var seen = new HashSet<UnityEngine.Object>();

        foreach (PRSDKDatabasePresetItem item in section.items)
        {
            UnityEngine.Object asset = Resolve(item, expectedType, section.label, report);
            if (asset == null)
                continue;

            if (!seen.Add(asset))
            {
                report.AddIssue(
                    PRSDKDatabasePresetSeverity.Warning,
                    section.label,
                    $"«{item.name}» указан в наборе дважды — лишний пропущен.");
                continue;
            }

            resolved.Assets.Add(asset);
        }

        CollectOutgoing(resolved, catalog);

        return resolved;
    }

    /// <summary>
    /// Запоминает, что лежит в каталоге сверх набора.
    /// </summary>
    /// <remarks>
    /// Набор мог быть сохранён до того, как в базу добавили новые предметы. Что с ними
    /// станет, решает режим применения, поэтому здесь только факт, без оценки.
    /// </remarks>
    private static void CollectOutgoing(
        PRSDKDatabasePresetResolvedSection resolved,
        SerializedProperty catalog)
    {
        var incoming = new HashSet<UnityEngine.Object>(resolved.Assets);

        for (int index = 0; index < catalog.arraySize; index++)
        {
            UnityEngine.Object current = catalog.GetArrayElementAtIndex(index).objectReferenceValue;

            if (current != null && !incoming.Contains(current))
                resolved.Outgoing.Add(current);
        }
    }

    /// <summary>
    /// Ищет ассет по описанию из набора.
    /// </summary>
    /// <remarks>
    /// Три попытки по убыванию надёжности: GUID, затем путь, затем имя вместе с типом.
    /// Каждая следующая — повод предупредить, потому что совпадение уже не строгое.
    /// </remarks>
    private static UnityEngine.Object Resolve(
        PRSDKDatabasePresetItem item,
        string expectedType,
        string sectionLabel,
        PRSDKDatabasePresetReport report)
    {
        UnityEngine.Object asset = LoadByGuid(item);

        if (asset == null && !string.IsNullOrEmpty(item.path))
        {
            asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);

            if (asset != null)
            {
                report.AddIssue(
                    PRSDKDatabasePresetSeverity.Warning,
                    sectionLabel,
                    $"«{item.name}» найден по пути, GUID не совпал — проверьте, тот ли это ассет.");
            }
        }

        if (asset == null)
            asset = FindByName(item);

        if (asset == null)
        {
            report.AddIssue(
                PRSDKDatabasePresetSeverity.Error,
                sectionLabel,
                $"«{item.name}» ({item.type}) не найден в проекте — пропущен.");
            return null;
        }

        if (!string.IsNullOrEmpty(expectedType) && !Matches(asset, expectedType))
        {
            report.AddIssue(
                PRSDKDatabasePresetSeverity.Error,
                sectionLabel,
                $"«{item.name}» оказался типа {asset.GetType().Name}, а каталогу нужен {expectedType} — пропущен.");
            return null;
        }

        return asset;
    }

    private static UnityEngine.Object LoadByGuid(PRSDKDatabasePresetItem item)
    {
        if (string.IsNullOrEmpty(item.guid))
            return null;

        string path = AssetDatabase.GUIDToAssetPath(item.guid);
        if (string.IsNullOrEmpty(path))
            return null;

        // Вложенные объекты: GUID один на файл, различает их локальный идентификатор.
        foreach (UnityEngine.Object candidate in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (candidate == null)
                continue;

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long localId)
                && localId == item.localId)
            {
                return candidate;
            }
        }

        return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
    }

    /// <summary>
    /// Ищет ассет по имени и типу — последняя попытка.
    /// </summary>
    private static UnityEngine.Object FindByName(PRSDKDatabasePresetItem item)
    {
        if (string.IsNullOrEmpty(item.name) || string.IsNullOrEmpty(item.type))
            return null;

        string[] guids = AssetDatabase.FindAssets($"\"{item.name}\" t:{item.type}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var candidate = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (candidate != null && candidate.name == item.name)
                return candidate;
        }

        return null;
    }

    private static bool Matches(UnityEngine.Object asset, string expectedType)
    {
        for (Type type = asset.GetType(); type != null; type = type.BaseType)
        {
            if (type.Name == expectedType)
                return true;
        }

        return false;
    }

    #endregion

    #region Применение

    /// <summary>
    /// Записывает разобранный набор в базу.
    /// </summary>
    /// <remarks>
    /// Каталоги, которых в наборе не было, не трогаются вовсе — независимо от режима.
    /// </remarks>
    /// <param name="mode">Что делать с тем, что уже лежит в каталогах.</param>
    /// <returns>Число изменённых каталогов.</returns>
    public static int Apply(
        PRSDKDatabasePresetReport report,
        PRSDKDatabase database,
        PRSDKDatabasePresetApplyMode mode)
    {
        var serialized = new SerializedObject(database);
        int changed = 0;

        foreach (PRSDKDatabasePresetResolvedSection section in report.Sections)
        {
            if (section.Skipped)
                continue;

            SerializedProperty catalog = serialized.FindProperty(section.Path);
            if (catalog == null || !catalog.isArray)
                continue;

            List<UnityEngine.Object> result = mode == PRSDKDatabasePresetApplyMode.Replace
                ? section.Assets
                : Merge(section);

            // При дополнении каталог мог не измениться вовсе - тогда и трогать его незачем.
            if (mode == PRSDKDatabasePresetApplyMode.Merge && result.Count == catalog.arraySize)
                continue;

            catalog.ClearArray();

            for (int index = 0; index < result.Count; index++)
            {
                catalog.InsertArrayElementAtIndex(index);
                catalog.GetArrayElementAtIndex(index).objectReferenceValue = result[index];
            }

            changed++;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        return changed;
    }

    /// <summary>
    /// Складывает существующий состав каталога с набором.
    /// </summary>
    /// <remarks>
    /// Существующее идёт первым и сохраняет порядок: дополнение не должно перетасовывать
    /// то, что уже собрано.
    /// </remarks>
    private static List<UnityEngine.Object> Merge(PRSDKDatabasePresetResolvedSection section)
    {
        var result = new List<UnityEngine.Object>(section.Outgoing);
        var seen = new HashSet<UnityEngine.Object>(section.Outgoing);

        foreach (UnityEngine.Object asset in section.Assets)
        {
            if (seen.Add(asset))
                result.Add(asset);
        }

        return result;
    }

    #endregion

    #region Обход базы

    /// <summary>
    /// Перечисляет каталоги базы — массивы ссылок на ассеты.
    /// </summary>
    /// <remarks>
    /// По форме, а не по типу: каталоги в базе не только <c>Database&lt;T&gt;</c>, оружие,
    /// например, хранит несколько списков рядом. Любой массив ссылок — каталог.
    /// </remarks>
    public static IEnumerable<SerializedProperty> EnumerateCatalogs(SerializedObject serialized)
    {
        SerializedProperty iterator = serialized.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            bool isCatalog = iterator.isArray
                && iterator.propertyType != SerializedPropertyType.String
                && IsObjectArray(iterator);

            // Внутрь найденного каталога заходить незачем: элементы читаются по индексу,
            // а обход сотен ссылок по одной заметно тормозит на больших базах.
            enterChildren = !isCatalog;

            if (isCatalog)
                yield return iterator.Copy();
        }
    }

    private static bool IsObjectArray(SerializedProperty property)
    {
        // arrayElementType для ссылок выглядит как PPtr<$HatDefinition>; у списков структур
        // и примитивов - иначе, и такие массивы каталогами не считаются.
        return property.arrayElementType != null && property.arrayElementType.StartsWith("PPtr<");
    }

    /// <summary>
    /// Возвращает тип элементов каталога.
    /// </summary>
    /// <remarks>
    /// Unity сообщает его как <c>PPtr&lt;$HatDefinition&gt;</c>; у встроенных типов доллара
    /// нет. Вырезается ровно середина: обрезать по набору символов нельзя, иначе у типа
    /// вроде <c>PetDefinition</c> отъелась бы первая буква.
    /// </remarks>
    private static string ExtractElementType(SerializedProperty property)
    {
        string raw = property.arrayElementType;

        if (string.IsNullOrEmpty(raw) || !raw.StartsWith("PPtr<"))
            return string.Empty;

        int start = raw.IndexOf('$');
        if (start < 0)
            start = raw.IndexOf('<');

        int end = raw.LastIndexOf('>');

        return start >= 0 && end > start ? raw.Substring(start + 1, end - start - 1) : string.Empty;
    }

    /// <summary>
    /// Собирает читаемое имя каталога из пути свойства.
    /// </summary>
    private static string BuildLabel(SerializedObject serialized, SerializedProperty catalog)
    {
        var parts = new List<string>();
        string[] segments = catalog.propertyPath.Split('.');
        string current = string.Empty;

        foreach (string segment in segments)
        {
            current = string.IsNullOrEmpty(current) ? segment : $"{current}.{segment}";

            // Служебные звенья пути ничего не сообщают человеку.
            if (segment == "data" || segment == "Array")
                continue;

            SerializedProperty property = serialized.FindProperty(current);
            if (property != null)
                parts.Add(property.displayName);
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : catalog.propertyPath;
    }

    #endregion
}
