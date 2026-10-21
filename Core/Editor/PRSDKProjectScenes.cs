using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Список сцен сборки по проекту: стартовая сцена, за ней сцена игры проекта.
/// </summary>
/// <remarks>
/// Стартовая сцена — та, что сейчас первая в списке сборки: с неё запускается игра (<c>PlayFromBootstrap</c>
/// берёт номер 0), сцена игры — сразу за ней: стартовая грузит её номером <c>Bootstrap.GameSceneIndex</c> (1).
/// Остальные сцены остаются в списке выключенными, порядок их не меняется:
/// чтобы вернуть прежнюю игру, достаточно сделать активным её проект.
/// <para>
/// Проект без сцены (<see cref="PRSDKProject.GameSceneGuid"/> пуст) список не трогает. При смене проекта его сцена
/// ещё и открывается (<see cref="OpenGameScene"/>), изменённые открытые сцены перед этим сохраняются.
/// </para>
/// </remarks>
public static class PRSDKProjectScenes
{
    /// <summary>
    /// Номер сцены игры в сборке: сразу за стартовой.
    /// </summary>
    public const int GameSceneBuildIndex = 1;

    /// <summary>
    /// Сверяет сборку с проектом: под номером, который грузит стартовая сцена, должна стоять сцена игры проекта.
    /// </summary>
    /// <returns><c>null</c> — всё сходится или у проекта нет сцены; иначе — что не так.</returns>
    public static string CheckBuild(PRSDKProject project, IReadOnlyList<string> enabledScenes)
    {
        if (project == null || string.IsNullOrEmpty(project.GameSceneGuid))
            return null;

        string gamePath = AssetDatabase.GUIDToAssetPath(project.GameSceneGuid);
        if (string.IsNullOrEmpty(gamePath))
            return $"сцена игры проекта «{project.Title}» не найдена (GUID {project.GameSceneGuid})";

        int gameIndex = GameSceneIndexOf(project);
        string loaded = gameIndex < enabledScenes.Count ? enabledScenes[gameIndex] : null;
        if (loaded == gamePath)
            return null;

        return $"стартовая сцена грузит номер {gameIndex} ({(loaded ?? "такой сцены в сборке нет")}), а сцена игры проекта «{project.Title}» — {gamePath}. " +
               "Поправьте Bootstrap → Game Scene Index или сделайте проект активным заново в окне проектов";
    }

    private static int GameSceneIndexOf(PRSDKProject project) =>
        project.Settings != null && project.Settings.Bootstrap != null ? project.Settings.Bootstrap.GameSceneIndex : GameSceneBuildIndex;

    /// <summary>
    /// Ставит сцену игры проекта в сборку.
    /// </summary>
    /// <returns><c>false</c> — у проекта нет сцены или её не нашлось.</returns>
    public static bool Apply(PRSDKProject project)
    {
        if (project == null || string.IsNullOrEmpty(project.GameSceneGuid))
            return false;

        string gamePath = AssetDatabase.GUIDToAssetPath(project.GameSceneGuid);
        if (string.IsNullOrEmpty(gamePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(gamePath) == null)
        {
            Debug.LogWarning($"[PRUnitySDK] У проекта «{project.Title}» сцена игры не найдена (GUID {project.GameSceneGuid}): список сборки не тронут.");
            return false;
        }

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        string bootstrapPath = scenes.Count > 0 ? scenes[0].path : null;
        if (string.IsNullOrEmpty(bootstrapPath) || bootstrapPath == gamePath)
        {
            Debug.LogWarning($"[PRUnitySDK] Первой в списке сборки должна стоять стартовая сцена, а там «{bootstrapPath}»: список сборки не тронут.");
            return false;
        }

        // Номер в сборке считается по включённым сценам: сцена игры — вторая включённая, то есть номер 1.
        // Другой номер в настройках сборку остановит (PRSDKProjectScenesBuildCheck): список всё равно строится,
        // иначе в нём осталась бы сцена прежней игры.
        int gameIndex = GameSceneIndexOf(project);
        if (gameIndex != GameSceneBuildIndex)
            Debug.LogError($"[PRUnitySDK] У проекта «{project.Title}» стартовая сцена грузит сцену номер {gameIndex}, а сцена игры встаёт номером {GameSceneBuildIndex}: " +
                           "поправьте Bootstrap → Game Scene Index, иначе сборка остановится.");

        var result = new List<EditorBuildSettingsScene> { new(bootstrapPath, true), new(gamePath, true) };
        result.AddRange(scenes.Skip(1).Where(scene => scene.path != gamePath).Select(scene => new EditorBuildSettingsScene(scene.path, false)));

        if (Same(scenes, result))
            return true;

        EditorBuildSettings.scenes = result.ToArray();
        Debug.Log($"[PRUnitySDK] Сцены сборки по проекту «{project.Title}»: {bootstrapPath} → {gamePath}.");
        return true;
    }

    /// <summary>
    /// Открывает сцену игры проекта, сохранив изменённые открытые сцены.
    /// </summary>
    /// <remarks>
    /// Идёт следующим кадром редактора: смену проекта сообщают из <c>OnValidate</c> и отмены, а оттуда сцены
    /// не открывают. Сцена без файла (Untitled) молча не сохранится — для неё Unity сама спросит путь, отказ
    /// оставляет открытым прежнее. В Play и в уже открытой сцене проекта ничего не делается.
    /// </remarks>
    public static void OpenGameScene(PRSDKProject project)
    {
        if (project == null || string.IsNullOrEmpty(project.GameSceneGuid))
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // За кадр проект могли сменить ещё раз (или вернуть прежний): открывается сцена только текущего.
            if (PRSDKActiveProject.Instance == null || PRSDKActiveProject.Instance.Project != project)
                return;

            string gamePath = AssetDatabase.GUIDToAssetPath(project.GameSceneGuid);
            if (string.IsNullOrEmpty(gamePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(gamePath) == null)
                return;

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == gamePath)
                return;

            var dirty = new List<UnityEngine.SceneManagement.Scene>();
            bool untitled = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isDirty) continue;
                if (string.IsNullOrEmpty(scene.path)) untitled = true;
                else dirty.Add(scene);
            }

            if (dirty.Count > 0 && !EditorSceneManager.SaveScenes(dirty.ToArray()))
            {
                Debug.LogWarning($"[PRUnitySDK] Открытые сцены не сохранились — сцена проекта «{project.Title}» не открыта.");
                return;
            }

            if (untitled && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Single);
            Debug.Log($"[PRUnitySDK] Открыта сцена проекта «{project.Title}»: {gamePath}.");
        };
    }

    private static bool Same(List<EditorBuildSettingsScene> a, List<EditorBuildSettingsScene> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i].path != b[i].path || a[i].enabled != b[i].enabled) return false;
        return true;
    }
}
