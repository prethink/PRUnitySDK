#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitializeSteam()
    {
        isOverriden = true;
    }

    /// <remarks>
    /// Steam запускается синхронно: к сборке SDK он уже поднят или известно, что его нет.
    /// </remarks>
    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnableSteam()
    {
        SteamPlatform.Init();
        platformLanguageCode = SteamLanguageManager.StartLanguage();
        InitializeSDK();
    }
}
#endif
#endif
