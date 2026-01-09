using System.IO;
using UnityEditor;
using UnityEngine;

public abstract class ResourceScriptableObject : ScriptableObject
{
    public static readonly string RESOURCES_PATH = "Assets/Resources";

    public static T Create<T>(bool logIfCreated = false, string resourcesPath = null) 
        where T : ResourceScriptableObject
    {
        var assetFolderPath = string.IsNullOrEmpty(resourcesPath) 
            ? GetSelectedFolderPath() 
            : resourcesPath;

        if (string.IsNullOrEmpty(assetFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select a folder in the Project window.",
                "Ok");
            return null;
        }

        if (Path.GetFileName(assetFolderPath) != "Resources")
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"{typeof(T).Name} must be created inside a folder named 'Resources'.",
                "Ok");
            return null;
        }

        var existing = FindInResources<T>();
        if (existing != null)
        {
            if (logIfCreated)
                PRLog.WriteWarning(typeof(T), $"{typeof(T).Name} already exists in Resources, returning existing instance.");
            return existing;
        }


        return CreateSettingsInternal<T>(assetFolderPath, logIfCreated);
    }

    /// <summary>
    /// Ищет ScriptableObject указанного типа в Resources и возвращает его
    /// </summary>
    private static T FindInResources<T>() where T : ResourceScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (IsInResourcesFolder(path))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    return asset;
            }
        }

        return null;
    }

    private static bool IsInResourcesFolder(string assetPath)
    {
        return assetPath.Contains("/Resources/");
    }

    private static T CreateSettingsInternal<T>(string assetFolderPath, bool logIfCreated) 
        where T : ResourceScriptableObject
    {
        var fullPath = Path.Combine(assetFolderPath, typeof(T).Name + ".asset")
            .Replace("\\", "/");

        var settings = CreateInstance<T>();
        settings.SetDefaultSettings();

        AssetDatabase.CreateAsset(settings, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = settings;

        if (logIfCreated)
        {
            EditorUtility.DisplayDialog(
                "Warning",
                $"Created {typeof(T).Name} at '{fullPath}'. A configuration file is required for correct operation.",
                "Ok");
        }

        return settings;
    }

    /// <summary>
    /// Возвращает путь к выбранной папке в Project Window
    /// </summary>
    private static string GetSelectedFolderPath()
    {
        var obj = Selection.activeObject;

        if (obj == null)
            return null;

        var path = AssetDatabase.GetAssetPath(obj);

        if (string.IsNullOrEmpty(path))
            return null;

        if (AssetDatabase.IsValidFolder(path))
            return path;

        return Path.GetDirectoryName(path);
    }

    protected abstract void SetDefaultSettings();
}
