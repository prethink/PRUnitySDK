using System.IO;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectSingleton<T> : ScriptableObject 
    where T : ScriptableObjectSingleton<T>
{
    /// <summary>
    /// Где живут данные игры.
    /// </summary>
    /// <remarks>
    /// Отдельно от кода SDK намеренно: обновление SDK не должно задевать содержимое игры,
    /// а содержимое игры — попадать в SDK. Папка Resources внутри неё временная: как только
    /// данные перечислены в <see cref="PRSDKProject"/>, в ресурсах остаётся один указатель
    /// на активный проект.
    /// </remarks>
    public const string PATCH_ASSETS = "Assets/PRUnityData";

    public const string CORE_FOLDER = "PRUnitySDK";

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance != null)
                return instance;

            // Сначала спрашиваем текущий проект: у каждой игры свои база, настройки
            // и префабы, и лежат они вне Resources — иначе в сборку тянулись бы данные
            // всех игр разом.
            instance = PRSDKActiveProject.ResolveAsset<T>();

            if (instance != null)
                return instance;

            var fileName = typeof(T).Name;

            // Проекта нет или в нём этой части не задано — прежний путь через ресурсы.
            instance = Resources.Load<T>($"{PRUnitySDK.ResourcePaths.CorePath}/{fileName}");

            if (instance != null)
                return instance;

#if UNITY_EDITOR
            instance = CreateInEditor(fileName);
#else
            Debug.LogError($"[Singleton] {fileName} not found in Resources!");
#endif

            return instance;
        }
    }

#if UNITY_EDITOR
    private static T CreateInEditor(string fileName)
    {
        string directory = $"{PATCH_ASSETS}/Resources/{CORE_FOLDER}";
        string path = $"{directory}/{fileName}.asset";

        // Файл есть, а Resources его не видит — база ассетов ещё не готова (первый импорт, загрузка домена).
        // Создать заново значило бы затереть данные, например выбранный активный проект. Пока ассет
        // не импортирован, вернётся null, и синглтон поищет его снова при следующем обращении.
        if (File.Exists(path))
            return AssetDatabase.LoadAssetAtPath<T>(path);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Созданный объект и есть ассет. Перечитывать его через Resources нельзя: во время импорта
        // загрузка возвращает null.
        T created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);

        created.SetDefaultSettings();
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssetIfDirty(created);

        return created;
    }
#endif

    /// <summary>
    /// Забывает найденный ассет.
    /// </summary>
    /// <remarks>
    /// Нужно при смене проекта: синглтон держит ссылку до перезапуска, и без сброса
    /// редактор продолжал бы показывать данные прежней игры.
    /// </remarks>
    public static void ResetInstance()
    {
        instance = null;
    }

    /// <summary>
    /// Установить настройки по умолчанию.
    /// </summary>
    protected virtual void SetDefaultSettings()
    {
        this.RunMethodHooks(MethodHookStage.Initializing);
    }

}