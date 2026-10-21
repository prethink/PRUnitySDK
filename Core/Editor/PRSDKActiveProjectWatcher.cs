using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Следит за активным проектом в редакторе и переводит синглтоны базы и настроек на то, что в нём сейчас лежит.
/// </summary>
/// <remarks>
/// Окно проектов при переключении само сбрасывает кеш синглтонов. Но указатель меняют и мимо него — Undo, правкой
/// в инспекторе, — а в самом активном проекте можно заменить базу или настройки, не трогая указатель. Без сброса
/// <see cref="PRSDKDatabase"/>.Instance и <see cref="PRSDKSettings"/>.Instance остались бы прежними, и окна базы
/// и настроек, которые сверяются с ними на отрисовке, продолжили бы править чужие данные. Поэтому сравниваются
/// не только проект, но и его база и настройки, и сбрасывается только сменившийся синглтон.
/// <para>
/// Во время игры проект не меняется: игра уже держит его базу и настройки, а сохранения площадок переключаются только
/// вне игры (<see cref="PRSDKProjectSaves"/>) — игра читала бы данные одного проекта и сохранение другого. Сменённый
/// указатель возвращается прежнему проекту; правки внутри проекта (база, настройки, сцена) применяются после выхода
/// из игры.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRSDKActiveProjectWatcher
{
    private static PRSDKProject lastProject;
    private static PRSDKDatabase lastDatabase;
    private static PRSDKSettings lastSettings;
    private static string lastScene;

    static PRSDKActiveProjectWatcher()
    {
        Remember();
        PRSDKActiveProject.EditedInEditor += Check;
        PRSDKProject.EditedInEditor += Check;
        Undo.undoRedoPerformed += Check;
        // После загрузки сверить список сборки: смену могли не применить до перезагрузки (сбой, перезапуск). Сцену
        // не открываем — иначе каждая компиляция уводила бы из открытой тестовой сцены.
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && CurrentProject != null)
                PRSDKProjectScenes.Apply(CurrentProject);
        };
        EditorApplication.playModeStateChanged += state =>
        {
            // Перед игрой — довести смену, о которой ещё не сообщили: иначе в игре её сочли бы сменой посреди игры.
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
                Apply();
        };
    }

    private static PRSDKProject CurrentProject => PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;

    private static void Remember()
    {
        lastProject = CurrentProject;
        lastDatabase = lastProject != null ? lastProject.Database : null;
        lastSettings = lastProject != null ? lastProject.Settings : null;
        lastScene = lastProject != null ? lastProject.GameSceneGuid : null;
    }

    private static void Check()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            KeepProjectWhilePlaying();
        else
            Apply();
    }

    private static void Apply()
    {
        PRSDKProject project = CurrentProject;
        bool databaseChanged = (project != null ? project.Database : null) != lastDatabase;
        bool settingsChanged = (project != null ? project.Settings : null) != lastSettings;

        bool projectChanged = project != lastProject;
        bool sceneChanged = (project != null ? project.GameSceneGuid : null) != lastScene;

        if (!projectChanged && !databaseChanged && !settingsChanged && !sceneChanged)
            return;

        Remember();

        // Смена проекта сбрасывает оба: неполный проект берёт недостающее из ресурсов, и прежний синглтон мог быть оттуда.
        if (projectChanged || databaseChanged)
            ScriptableObjectSingleton<PRSDKDatabase>.ResetInstance();
        if (projectChanged || settingsChanged)
            ScriptableObjectSingleton<PRSDKSettings>.ResetInstance();

        // Сцена игры из проекта встаёт в сборку за стартовой: и при смене проекта, и когда её выбрали у активного.
        if ((projectChanged || sceneChanged) && project != null)
            PRSDKProjectScenes.Apply(project);

        // Сменили проект — сразу открыть его сцену, сохранив текущие.
        if (projectChanged && project != null)
            PRSDKProjectScenes.OpenGameScene(project);

        InternalEditorUtility.RepaintAllViews();
    }

    /// <summary>
    /// Возвращает указатель проекту, с которым идёт игра.
    /// </summary>
    private static void KeepProjectWhilePlaying()
    {
        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;
        if (pointer == null || pointer.Project == lastProject)
            return;

        string attempted = pointer.Project != null ? pointer.Project.Title : "пусто";
        pointer.SetProject(lastProject);
        Debug.LogError($"[PRUnitySDK] Проект SDK меняется вне игры: возвращён {(lastProject != null ? lastProject.Title : "пустой указатель")} " +
                       $"вместо {attempted}. Выйдите из Play и смените проект ещё раз.");
    }
}
