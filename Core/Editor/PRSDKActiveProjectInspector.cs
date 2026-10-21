using UnityEditor;

/// <summary>
/// Инспектор указателя проекта: во время игры только для чтения.
/// </summary>
/// <remarks>
/// Правка в инспекторе сразу меняет поле, а <see cref="PRSDKActiveProjectWatcher"/> узнаёт о ней только следующим
/// кадром редактора и тогда возвращает прежний проект. Чтобы игра и в этот кадр не видела чужой проект, поле в Play
/// не редактируется; откат наблюдателя остаётся на Undo и правки скриптом.
/// </remarks>
[CustomEditor(typeof(PRSDKActiveProject))]
public sealed class PRSDKActiveProjectInspector : Editor
{
    public override void OnInspectorGUI()
    {
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        if (playing)
            EditorGUILayout.HelpBox("Проект SDK меняется вне игры: выйдите из Play.", MessageType.Info);

        using (new EditorGUI.DisabledScope(playing))
            DrawDefaultInspector();
    }
}
