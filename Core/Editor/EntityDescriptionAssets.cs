using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Поиск ассетов описаний по проекту для редакторских инструментов.
/// </summary>
/// <remarks>
/// Описания и определения ищутся вместе: и то и другое - то, чем сущность представляется
/// игроку, и вопросы «какие описания есть» и «кто ими пользуется» к ним одинаковы.
/// </remarks>
public static class EntityDescriptionAssets
{
    /// <summary>
    /// Пути всех ассетов описаний и определений.
    /// </summary>
    public static IEnumerable<string> FindPaths()
    {
        return AssetDatabase.FindAssets($"t:{nameof(EntityMetadataBase)}")
            .Concat(AssetDatabase.FindAssets($"t:{nameof(ItemDefinitionBase)}"))
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Загруженные ассеты описаний и определений.
    /// </summary>
    public static IEnumerable<ScriptableObject> Find()
    {
        return Load(FindPaths());
    }

    /// <summary>
    /// Загруженные ассеты одного типа поиска.
    /// </summary>
    /// <param name="searchType">Имя типа для фильтра <c>t:</c> у AssetDatabase.</param>
    public static IEnumerable<ScriptableObject> Find(string searchType)
    {
        IEnumerable<string> paths = AssetDatabase.FindAssets($"t:{searchType}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(System.StringComparer.Ordinal);

        return Load(paths);
    }

    private static IEnumerable<ScriptableObject> Load(IEnumerable<string> paths)
    {
        return paths
            .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
            .Where(asset => asset is IEntityMetadata);
    }
}
