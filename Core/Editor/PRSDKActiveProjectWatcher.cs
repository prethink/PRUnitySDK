using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// Следит за активным проектом в редакторе и переводит синглтоны базы и настроек на то, что в нём сейчас лежит.
/// </summary>
/// <remarks>
/// Окно проектов при переключении само сбрасывает кеш синглтонов. Но указатель меняют и мимо него — Undo, правкой
/// в инспекторе, — а в самом активном проекте можно заменить базу или настройки, не трогая указатель. Без сброса
/// <see cref="PRSDKDatabase"/>.Instance и <see cref="PRSDKSettings"/>.Instance остались бы прежними, и окна базы
/// и настроек, которые сверяются с ними на отрисовке, продолжили бы править чужие данные. Поэтому сравниваются
/// не только проект, но и его база и настройки, и сбрасывается только сменившийся синглтон.
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
}
