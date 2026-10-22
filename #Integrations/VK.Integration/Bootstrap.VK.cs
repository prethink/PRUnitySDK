#if !PRSDK_DISABLE_VK
public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitializeVK()
    {
        isOverriden = true;
    }

    /// <remarks>
    /// <c>VKWebAppInit</c> шлёт шаблон страницы ещё до загрузки Unity: площадка ждёт его не дольше 30 секунд.
    /// Здесь мост дочитывает параметры запуска, профиль и сохранение.
    /// </remarks>
    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnableVK()
    {
        VKBridge.Initialized += OnVKInitialized;
        VKBridge.Init();

        if (VKBridge.State is VKBridgeState.Ready or VKBridgeState.Failed)
            OnVKInitialized();
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisableVK()
    {
        VKBridge.Initialized -= OnVKInitialized;
    }

    /// <summary>
    /// Запуск площадки закончился. Не поднялась — игра всё равно запускается, без неё.
    /// </summary>
    private void OnVKInitialized()
    {
        VKBridge.Initialized -= OnVKInitialized;

        if (!VKBridge.IsReady)
            PRLog.WriteWarning(this, "VK Bridge не запустился: SDK собирается без площадки.");

        platformLanguageCode = VKLanguageManager.StartLanguage();
        InitializeSDK();
    }
}
#endif
