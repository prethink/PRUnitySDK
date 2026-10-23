using UnityEngine;

public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_MONO_WINDOW_ESCAPE = 212;

    /// <summary>
    /// Заводит закрытие окон по Esc (<see cref="MonoWindowEscapeCloser"/>) на всю игру.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_MONO_WINDOW_ESCAPE)]
    private static void InitializeMonoWindowEscape()
    {
        if (Object.FindFirstObjectByType<MonoWindowEscapeCloser>(FindObjectsInactive.Include) != null)
            return;

        var host = new GameObject(nameof(MonoWindowEscapeCloser));
        Object.DontDestroyOnLoad(host);
        host.AddComponent<MonoWindowEscapeCloser>();
    }
}
