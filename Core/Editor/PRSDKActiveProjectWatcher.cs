using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// Следит за указателем активного проекта в редакторе и переводит на новый проект синглтоны базы и настроек.
/// </summary>
/// <remarks>
/// Окно проектов при переключении само сбрасывает кеш синглтонов. Но указатель меняют и мимо него — Undo, правкой
/// в инспекторе. Без сброса <see cref="PRSDKDatabase"/>.Instance и <see cref="PRSDKSettings"/>.Instance остались бы
/// прежними, и окна базы и настроек, которые сверяются с ними на отрисовке, продолжили бы править данные другой игры.
/// </remarks>
[InitializeOnLoad]
public static class PRSDKActiveProjectWatcher
{
    private static PRSDKProject lastProject;

    static PRSDKActiveProjectWatcher()
    {
        lastProject = CurrentProject;
        PRSDKActiveProject.EditedInEditor += Check;
        Undo.undoRedoPerformed += Check;
    }

    private static PRSDKProject CurrentProject => PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;

    private static void Check()
    {
        PRSDKProject current = CurrentProject;
        if (current == lastProject)
            return;

        lastProject = current;
        ScriptableObjectSingleton<PRSDKDatabase>.ResetInstance();
        ScriptableObjectSingleton<PRSDKSettings>.ResetInstance();
        InternalEditorUtility.RepaintAllViews();
    }
}
