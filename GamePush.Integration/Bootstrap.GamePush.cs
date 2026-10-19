#if !PRSDK_DISABLE_GAMEPUSH
using GamePush;

public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitializeGP()
    {
        isOverriden = true;
    }

    /// <remarks>
    /// GamePush поднимается сам, до первой сцены (<c>GP_Initialization</c>), и о готовности сообщает
    /// событием без повтора. Если он успел раньше загрузчика, сборка SDK начинается сразу.
    /// </remarks>
    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnableGP()
    {
        if (GP_Init.isReady)
        {
            InitializeFromPlatform();
        }
        else
        {
            GP_Init.OnReady += InitializeFromPlatform;
            GP_Init.OnError += InitializeWithoutPlatform;
        }

        // Отписки нет намеренно: загрузчик исчезает вместе со своей сценой раньше сигнала.
        GameShownSignal.SubscribeOnReady(ReportGameReady);
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisableGP()
    {
        GP_Init.OnReady -= InitializeFromPlatform;
        GP_Init.OnError -= InitializeWithoutPlatform;
    }

    /// <summary>
    /// Площадка поднялась: язык игрока уже известен, и экран загрузки можно подписать до сборки SDK.
    /// </summary>
    private void InitializeFromPlatform()
    {
        platformLanguageCode = GP_Language.CurrentISO();
        InitializeSDK();
    }

    /// <summary>
    /// Площадка не поднялась: игра всё равно должна запуститься.
    /// </summary>
    /// <remarks>
    /// Сервисы GamePush в этом случае не отвечают; те из них, что обращаются к площадке, проверяют
    /// <c>GP_Init.isReady</c> и работают как без неё.
    /// </remarks>
    private void InitializeWithoutPlatform()
    {
        PRLog.WriteWarning(this, "GamePush не запустился: SDK собирается без площадки.");
        InitializeSDK();
    }

    /// <summary>
    /// Сообщает площадке, что игра загружена и игрок может играть.
    /// </summary>
    /// <remarks>
    /// Ждёт сигнала показа игры: после готовности SDK под экраном ещё грузится сцена.
    /// </remarks>
    private static void ReportGameReady()
    {
        if (GP_Init.isReady)
            GP_Game.GameReady();
    }
}
#endif
