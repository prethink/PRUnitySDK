using UnityEngine;

public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_UI_CLICK_SOUND = 210;

    /// <summary>
    /// Заводит общий щелчок нажатия (<see cref="UIClickSoundPlayer"/>) на всю игру.
    /// </summary>
    /// <remarks>
    /// Звук и громкость берутся из настроек при каждом нажатии, поэтому выключатель
    /// в <see cref="PRSDKSettings.UIClickSound"/> действует сразу.
    /// </remarks>
    [MethodHook(MethodHookStage.SDK, PRIORITY_UI_CLICK_SOUND)]
    private static void InitializeUIClickSound()
    {
        if (Object.FindObjectOfType<UIClickSoundPlayer>(true) != null)
            return;

        var host = new GameObject(nameof(UIClickSoundPlayer));
        Object.DontDestroyOnLoad(host);
        host.AddComponent<UIClickSoundPlayer>();
    }
}
