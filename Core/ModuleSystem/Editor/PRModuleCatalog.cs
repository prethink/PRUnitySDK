using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

/// <summary>
/// Находит модули проекта по манифестам и раскладывает по ним скрипты.
/// </summary>
public static class PRModuleCatalog
{
    /// <summary>
    /// Папка публичного SDK.
    /// </summary>
    public const string PublicRoot = "Assets/PRUnitySDK/";

    /// <summary>
    /// Папка приватного SDK.
    /// </summary>
    public const string PrivateRoot = "Assets/PRUnitySDKPrivate/";

    /// <summary>
    /// Собирает все модули проекта.
    /// </summary>
    /// <remarks>
    /// Скрипт принадлежит модулю с самой глубокой папкой, в которой он лежит.
    /// Пустой идентификатор в манифесте заполняется по имени папки и сохраняется.
    /// </remarks>
    public static List<PRModuleInfo> Load()
    {
        var modules = new List<PRModuleInfo>();
        bool manifestChanged = false;

        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(PRModuleManifest)))
        {
            var manifest = AssetDatabase.LoadAssetAtPath<PRModuleManifest>(AssetDatabase.GUIDToAssetPath(guid));
            if (manifest == null)
                continue;

            manifestChanged |= manifest.TryFillIdFromFolder();
            string folder = manifest.FolderPath;
            modules.Add(new PRModuleInfo(manifest, folder, GetLayer(folder)));
        }

        if (manifestChanged)
            AssetDatabase.SaveAssets();

        // Глубокие папки первыми: так ближайший модуль находится первым совпадением.
        modules.Sort((left, right) => right.Folder.Length.CompareTo(left.Folder.Length));

        foreach (PRModuleInfo module in modules)
            module.Parent = modules.FirstOrDefault(other => other != module && IsInside(module.Folder, other.Folder));

        CollectScripts(modules);
        LinkDependencies(modules);

        modules.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
        return modules;
    }

    /// <summary>
    /// Модуль, которому принадлежит файл.
    /// </summary>
    /// <returns><c>null</c>, если файл лежит вне модулей.</returns>
    public static PRModuleInfo FindOwner(IEnumerable<PRModuleInfo> modules, string assetPath)
    {
        PRModuleInfo owner = null;

        foreach (PRModuleInfo module in modules)
        {
            if (IsInside(assetPath, module.Folder) && (owner == null || module.Folder.Length > owner.Folder.Length))
                owner = module;
        }

        return owner;
    }

    /// <summary>
    /// Все зависимости модуля по цепочке, без него самого.
    /// </summary>
    public static HashSet<PRModuleInfo> CollectDependencies(PRModuleInfo module)
    {
        var result = new HashSet<PRModuleInfo>();
        var pending = new Stack<PRModuleInfo>(module.Dependencies);

        while (pending.Count > 0)
        {
            PRModuleInfo next = pending.Pop();
            if (next == module || !result.Add(next))
                continue;

            foreach (PRModuleInfo dependency in next.Dependencies)
                pending.Push(dependency);
        }

        return result;
    }

    /// <summary>
    /// Скрипты проекта, которые Unity компилирует: без папок с <c>~</c> и скрытых.
    /// </summary>
    public static IEnumerable<string> EnumerateScripts(string folder)
    {
        if (!Directory.Exists(folder))
            yield break;

        foreach (string file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            if (!IsIgnoredByUnity(path))
                yield return path;
        }
    }

    private static void CollectScripts(List<PRModuleInfo> modules)
    {
        foreach (PRModuleInfo module in modules)
        {
            foreach (string script in EnumerateScripts(module.Folder))
            {
                if (FindOwner(modules, script) != module)
                    continue;

                module.Scripts.Add(script);

                if (!module.IsRequired && PRModuleGuard.ReadGuardId(script) != module.Id)
                    module.UnguardedScripts.Add(script);
            }
        }
    }

    private static void LinkDependencies(List<PRModuleInfo> modules)
    {
        var byManifest = modules.ToDictionary(module => module.Manifest);

        foreach (PRModuleInfo module in modules)
        {
            if (module.Parent != null)
                module.Dependencies.Add(module.Parent);

            foreach (PRModuleManifest manifest in module.Manifest.Dependencies)
            {
                if (manifest == null || !byManifest.TryGetValue(manifest, out PRModuleInfo dependency))
                {
                    module.MissingDependencyCount++;
                    continue;
                }

                if (dependency != module && !module.Dependencies.Contains(dependency))
                    module.Dependencies.Add(dependency);
            }
        }

        foreach (PRModuleInfo module in modules)
        {
            foreach (PRModuleInfo dependency in module.Dependencies)
                dependency.Dependents.Add(module);
        }
    }

    private static PRModuleLayer GetLayer(string folder)
    {
        if (IsInside(folder, PublicRoot.TrimEnd('/')))
            return PRModuleLayer.Public;

        return IsInside(folder, PrivateRoot.TrimEnd('/')) ? PRModuleLayer.Private : PRModuleLayer.Project;
    }

    private static bool IsInside(string path, string folder)
    {
        return path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredByUnity(string path)
    {
        foreach (string part in path.Split('/'))
        {
            if (part.EndsWith("~", StringComparison.Ordinal) || part.StartsWith(".", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
