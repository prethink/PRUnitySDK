#if !PRSDK_DISABLE_YG2
using YG;

public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitialize()
    {
        isOverriden = true;
    }

    [MethodHook(MethodHookStage.PostOnEnable)]
    private void OnEnableYG()
    {
        YG2.onGetSDKData += InitializeFromPlatform;
        //TODO:YG2.onDefaultSaves += InitializeSDK;
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisableYG()
    {
        YG2.onGetSDKData -= InitializeFromPlatform;
        //TODO:YG2.onDefaultSaves -= InitializeSDK;
    }

    /// <summary>
    /// Данные площадки пришли: язык игрока уже известен, и экран загрузки можно подписать до сборки SDK.
    /// </summary>
    private void InitializeFromPlatform()
    {
        platformLanguageCode = YG2.lang;
        InitializeSDK();
    }
}
#endif
