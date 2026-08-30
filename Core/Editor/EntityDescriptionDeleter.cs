using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Удаление описания и связанных с ним префабов.
/// </summary>
/// <remarks>
/// Оба действия необратимы, поэтому каждое спрашивает подтверждение и показывает полный
/// список того, что исчезнет. Молчаливое удаление здесь особенно опасно: описание держит
/// на себе ссылки с префабов, и «просто удалить» ломает их, ничего не сообщая.
/// </remarks>
public static class EntityDescriptionDeleter
{
    /// <summary>
    /// Удаляет только ассет описания.
    /// </summary>
    /// <remarks>
    /// Префабы, ссылавшиеся на него, останутся с пустой ссылкой: сущность потеряет имя
    /// и уедет в вид <c>Unknown</c>. Поэтому в подтверждении они перечислены поимённо.
    /// </remarks>
    /// <returns><c>true</c>, если ассет удалён.</returns>
    public static bool DeleteAsset(Object asset)
    {
        if (asset == null)
            return false;

        string path = AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(path))
            return false;

        IReadOnlyList<string> usages = EntityMetadataUsageIndex.GetUsages(asset);
        string message = $"Удалить «{asset.name}»?\n\n{path}";

        if (usages.Count > 0)
        {
            message += $"\n\nНа него ссылаются ({usages.Count}) - они останутся " +
                       $"без описания:\n{Describe(usages)}";
        }

        if (!EditorUtility.DisplayDialog("Удаление описания", message, "Удалить", "Отмена"))
            return false;

        if (!AssetDatabase.DeleteAsset(path))
        {
            Debug.LogError($"[EntityDescriptionDeleter] Не удалось удалить {path}.");
            return false;
        }

        EntityMetadataUsageIndex.Invalidate();

        return true;
    }

    /// <summary>
    /// Удаляет описание вместе со всеми префабами, которые на него ссылаются.
    /// </summary>
    /// <returns><c>true</c>, если что-то удалено.</returns>
    public static bool DeleteWithPrefabs(Object asset)
    {
        if (asset == null)
            return false;

        string path = AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(path))
            return false;

        IReadOnlyList<string> prefabs = EntityMetadataUsageIndex.GetPrefabs(asset);

        if (prefabs.Count == 0)
            return DeleteAsset(asset);

        IReadOnlyList<string> scenes = EntityMetadataUsageIndex.GetScenes(asset);

        string message =
            $"Удалить «{asset.name}» и все префабы, которые на него ссылаются?\n\n" +
            $"{path}\n\nПрефабы ({prefabs.Count}):\n{Describe(prefabs)}";

        // Сцены не удаляются никогда: описание там — деталь целого уровня, и убирать
        // уровень заодно с ней никто не просил. Но объекты в них останутся без описания,
        // и знать об этом надо до нажатия, а не после.
        if (scenes.Count > 0)
        {
            message +=
                $"\n\nСцены ({scenes.Count}) удалены НЕ будут, но сущности в них останутся " +
                $"без описания:\n{Describe(scenes)}";
        }

        message += "\n\nДействие необратимо.";

        if (!EditorUtility.DisplayDialog("Удаление описания и префабов", message, "Удалить всё", "Отмена"))
            return false;

        var failed = new List<string>();

        // Префабы удаляются первыми: если что-то пойдёт не так, описание останется
        // на месте и разбираться будет с чем.
        foreach (string prefabPath in prefabs)
        {
            if (!AssetDatabase.DeleteAsset(prefabPath))
                failed.Add(prefabPath);
        }

        if (failed.Count > 0)
        {
            Debug.LogError(
                $"[EntityDescriptionDeleter] Не удалось удалить префабы:\n{string.Join("\n", failed)}\n" +
                "Описание оставлено на месте.");
            EntityMetadataUsageIndex.Invalidate();
            return false;
        }

        if (!AssetDatabase.DeleteAsset(path))
        {
            Debug.LogError($"[EntityDescriptionDeleter] Префабы удалены, но {path} удалить не удалось.");
            EntityMetadataUsageIndex.Invalidate();
            return false;
        }

        EntityMetadataUsageIndex.Invalidate();

        return true;
    }

    /// <summary>
    /// Список путей для диалога.
    /// </summary>
    /// <remarks>
    /// Длинный список обрезается: в системном диалоге он всё равно не поместится,
    /// а решение принимается по первым строкам и по их количеству.
    /// </remarks>
    private static string Describe(IReadOnlyList<string> paths)
    {
        const int limit = 10;

        string listed = string.Join("\n", paths.Take(limit).Select(System.IO.Path.GetFileName));

        return paths.Count > limit
            ? $"{listed}\n… и ещё {paths.Count - limit}"
            : listed;
    }
}
