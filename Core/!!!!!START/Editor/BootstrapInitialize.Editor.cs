using UnityEngine.SceneManagement;

//public static class BootstrapInitializer
//{
//    // Флаг, чтобы инициализация сработала только один раз
//    private static bool _initialized = false;

//    // Этот метод сработает **до всех Awake**
//    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
//    private static void Initialize()
//    {
//        // Если уже инициализировано — выходим
//        if (_initialized)
//            return;

//        _initialized = true; // ставим флаг, чтобы больше не срабатывало
//        Debug.Log("Bootstrap: Инициализация до всех Awake");

//        // Всегда вызываем загрузку первой сцены, чтобы гарантировать, что она будет загружена.
//        // Это bootstrap сцена, которая может быть пустой или содержать только необходимые объекты для инициализации.
//        SceneManager.LoadScene(0);
//    }
//}


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromBootstrap
{
    static PlayFromBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Bootstrap.EditorStartScenePath = string.Empty;
            return;
        }

        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        // Берём сцену с индексом 0 из Build Settings
        var bootstrapPath = SceneUtility.GetScenePathByBuildIndex(0);

        Bootstrap.EditorStartScenePath = string.Empty;

        if (PRUnitySDK.Settings?.Bootstrap?.StartFromEditorScene == true)
        {
            var activeScene = SceneManager.GetActiveScene();

            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path) && activeScene.path != bootstrapPath)
                Bootstrap.EditorStartScenePath = activeScene.path;
            else
                UnityEngine.Debug.LogWarning("[Bootstrap] Откройте сохранённую игровую сцену перед Play. " +
                                             "Будет использован Game Scene Index.");
        }

        // Устанавливаем её как стартовую
        EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(bootstrapPath);
    }
}
#endif