using System.IO;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectSingleton<T> : ScriptableObject 
    where T : ScriptableObjectSingleton<T>
{
    public const string PATCH_ASSETS = "Assets/PRUnitySDK";

    public const string CORE_FOLDER = "PRUnitySDK";

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance != null)
                return instance;

            var fileName = typeof(T).Name;

            instance = Resources.Load<T>(fileName);

            if (instance != null)
                return instance;

            instance = ScriptableObject.CreateInstance<T>();

            string path = $"{PATCH_ASSETS}/Resources/{fileName}.asset";
            string directory = $"{PATCH_ASSETS}/Resources";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.Refresh();

            instance = Resources.Load<T>(fileName);

            instance.SetDefaultSettings();

            return instance;
        }
    }

    /// <summary>
    /// Установить настройки по умолчанию.
    /// </summary>
    protected virtual void SetDefaultSettings()
    {
        this.RunMethodHooks(MethodHookStage.Initializing);
    }

}