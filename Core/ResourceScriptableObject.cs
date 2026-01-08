using System.IO;
using UnityEditor;
using UnityEngine;

public abstract class ResourceScriptableObject : ScriptableObject
{
    protected static void CreateGameSettings<T>() 
        where T : ScriptableObject
    {
        var assetFolderPath = GetSelectedFolderPath();

        if (string.IsNullOrEmpty(assetFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select a folder in the Project window.",
                "Ok");
            return;
        }

        if (Path.GetFileName(assetFolderPath) != "Resources")
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"{typeof(T).Name} must be created inside a folder named 'Resources'.",
                "Ok");
            return;
        }

        // 🔍 Проверка по типу
        if (ExistsInResources<PRGameSettings>())
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"{typeof(T).Name} already exists in Resources.",
                "Ok");
            return;
        }


        CreateGameSettingsInternal<T>(assetFolderPath);
    }

    /// <summary>
    /// Проверяет, существует ли ScriptableObject указанного типа в Resources
    /// </summary>
    private static bool ExistsInResources<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (IsInResourcesFolder(path))
                return true;
        }

        return false;
    }

    private static bool IsInResourcesFolder(string assetPath)
    {
        return assetPath.Contains("/Resources/");
    }

    private static void CreateGameSettingsInternal<T>(string assetFolderPath) 
        where T : ScriptableObject
    {
        var fullPath = Path.Combine(assetFolderPath, typeof(T).Name + ".asset")
            .Replace("\\", "/");

        if (AssetDatabase.LoadAssetAtPath<T>(fullPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Asset already exists at:\n{fullPath}",
                "Ok");
            return;
        }

        var settings = CreateInstance<T>();

        AssetDatabase.CreateAsset(settings, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = settings;

        Debug.Log($"Created PRGameSettings at '{fullPath}'");
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
}
