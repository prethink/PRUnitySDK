#if !PRSDK_DISABLE_PIKABU
public partial class Bootstrap
{
    /// <summary>
    /// Язык экрана загрузки и первого запуска: площадка требует русский интерфейс по умолчанию.
    /// </summary>
    private const string PikabuLanguageCode = "ru";

    [OverrideBootstrap]
    public void OverrideInitializePKB()
    {
        isOverriden = true;
    }

    /// <remarks>
    /// SDK запускается здесь, а не до первой сцены: своего автозапуска у площадки нет. Шаблон страницы
    /// начинает запуск ещё при загрузке Unity, поэтому ответ обычно приходит сразу.
    /// </remarks>
    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnablePKB()
    {
        PikabuSDK.Initialized += OnPikabuInitialized;
        PikabuSDK.Init();

        if (PikabuSDK.State is PikabuSDKState.Ready or PikabuSDKState.Failed)
            OnPikabuInitialized();

        // Отписки нет намеренно: загрузчик исчезает вместе со своей сценой раньше сигнала.
        GameShownSignal.SubscribeOnReady(PikabuSDK.GameStarted);
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisablePKB()
    {
        PikabuSDK.Initialized -= OnPikabuInitialized;
    }

    /// <summary>
    /// Запуск SDK закончился. Не поднялся — игра всё равно запускается, без площадки.
    /// </summary>
    /// <remarks>
    /// Сервисы Пикабу в этом случае не отвечают: те, что обращаются к площадке, проверяют
    /// <see cref="PikabuSDK.IsReady"/> и работают как без неё.
    /// </remarks>
    private void OnPikabuInitialized()
    {
        PikabuSDK.Initialized -= OnPikabuInitialized;

        if (!PikabuSDK.IsReady)
            PRLog.WriteWarning(this, "SDK Пикабу не запустился: SDK собирается без площадки.");

        platformLanguageCode = PikabuLanguageManager.LoadSavedOrDefault();
        InitializeSDK();
    }
}
#endif
