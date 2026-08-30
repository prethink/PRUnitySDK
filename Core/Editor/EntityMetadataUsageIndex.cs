using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Кто каким описанием пользуется.
/// </summary>
/// <remarks>
/// Unity умеет отвечать только на прямой вопрос - «от чего зависит этот префаб».
/// Обратный вопрос, «кто ссылается на это описание», приходится собирать самому: обойти
/// префабы один раз и запомнить связи.
/// <para>
/// Обход не из дешёвых - префабов в проекте тысячи, - поэтому результат живёт до первой
/// правки ассетов: <see cref="EntityMetadataUsageWatcher"/> сбрасывает его, когда что-то
/// импортировалось, переместилось или удалилось.
/// </para>
/// </remarks>
public static class EntityMetadataUsageIndex
{
    private static Dictionary<string, List<string>> usages;

    /// <summary>
    /// Индекс собран и ещё не устарел.
    /// </summary>
    public static bool IsBuilt => usages != null;

    /// <summary>
    /// Префабы, ссылающиеся на описание.
    /// </summary>
    /// <remarks>
    /// Только префабы: сетка карточек показывает превью объекта, а у сцены его нет.
    /// Полный список - <see cref="GetUsages"/>.
    /// </remarks>
    /// <param name="asset">Описание.</param>
    /// <returns>Пути префабов; пусто, если описанием никто не пользуется.</returns>
    public static IReadOnlyList<string> GetPrefabs(Object asset)
    {
        return GetUsages(asset)
            .Where(path => path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Сцены, ссылающиеся на описание.
    /// </summary>
    /// <remarks>
    /// Отделены от префабов, потому что обращаться с ними приходится иначе: префаб,
    /// оставшийся без описания, можно удалить вместе с ним, а сцену - нет.
    /// </remarks>
    public static IReadOnlyList<string> GetScenes(Object asset)
    {
        return GetUsages(asset)
            .Where(path => path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Всё, что ссылается на описание: префабы и сцены.
    /// </summary>
    /// <param name="asset">Описание.</param>
    /// <returns>Пути ассетов; пусто, если описанием никто не пользуется.</returns>
    public static IReadOnlyList<string> GetUsages(Object asset)
    {
        if (asset == null)
            return System.Array.Empty<string>();

        string guid = ToGuid(asset);

        if (string.IsNullOrEmpty(guid))
            return System.Array.Empty<string>();

        usages ??= Build();

        return usages.TryGetValue(guid, out List<string> prefabs)
            ? prefabs
            : System.Array.Empty<string>();
    }

    /// <summary>
    /// Сбрасывает индекс - соберётся заново при следующем запросе.
    /// </summary>
    public static void Invalidate()
    {
        usages = null;
    }

    /// <summary>
    /// Обходит префабы и запоминает, кто на какие описания ссылается.
    /// </summary>
    /// <remarks>
    /// Зависимости берутся нерекурсивно: описание лежит прямо на сущности, а рекурсивный
    /// обход притянул бы ещё и всё, на что ссылается само описание.
    /// </remarks>
    private static Dictionary<string, List<string>> Build()
    {
        var result = new Dictionary<string, List<string>>();

        // Описания и определения индексируются вместе: и то и другое - то, чем сущность
        // представляется игроку, и вопрос «кто этим пользуется» к ним одинаковый.
        var metadataPaths = new HashSet<string>(
            AssetDatabase.FindAssets($"t:{nameof(EntityMetadataBase)}")
                .Concat(AssetDatabase.FindAssets($"t:{nameof(ItemDefinitionBase)}"))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path)));

        if (metadataPaths.Count == 0)
            return result;

        // Сцены обходятся наравне с префабами: сущность так же часто стоит прямо в сцене,
        // и без них ответ «этим описанием никто не пользуется» был бы неверным.
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab")
            .Concat(AssetDatabase.FindAssets("t:Scene"))
            .ToArray();

        try
        {
            for (int index = 0; index < prefabGuids.Length; index++)
            {
                if (index % 100 == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Описания сущностей",
                        "Поиск префабов и сцен, использующих описания...",
                        (float)index / prefabGuids.Length))
                {
                    // Отмена оставляет то, что успели собрать: неполный список честнее
                    // пустого, а кнопка «Обновить» рядом.
                    break;
                }

                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[index]);

                if (string.IsNullOrEmpty(prefabPath))
                    continue;

                foreach (string dependency in AssetDatabase.GetDependencies(prefabPath, false))
                {
                    if (!metadataPaths.Contains(dependency))
                        continue;

                    string metadataGuid = AssetDatabase.AssetPathToGUID(dependency);

                    if (string.IsNullOrEmpty(metadataGuid))
                        continue;

                    if (!result.TryGetValue(metadataGuid, out List<string> prefabs))
                    {
                        prefabs = new List<string>();
                        result.Add(metadataGuid, prefabs);
                    }

                    prefabs.Add(prefabPath);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (List<string> prefabs in result.Values)
            prefabs.Sort(System.StringComparer.OrdinalIgnoreCase);

        return result;
    }

    private static string ToGuid(Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);

        return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
    }
}

/// <summary>
/// Сбрасывает индекс использований при изменении ассетов.
/// </summary>
/// <remarks>
/// Без сброса список префабов показывал бы состояние на момент первого запроса: только что
/// созданный префаб в нём не появился бы, а удалённый остался бы висеть.
/// </remarks>
public class EntityMetadataUsageWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!EntityMetadataUsageIndex.IsBuilt)
            return;

        if (importedAssets.Length == 0 &&
            deletedAssets.Length == 0 &&
            movedAssets.Length == 0)
        {
            return;
        }

        EntityMetadataUsageIndex.Invalidate();
    }
}
