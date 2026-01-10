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
        YG2.onGetSDKData += InitializeSDK;
        //TODO:YG2.onDefaultSaves += InitializeSDK;
    }

    [MethodHook(MethodHookStage.PostOnDisable)]
    private void OnDisableYG()
    {
        YG2.onGetSDKData -= InitializeSDK;
        //TODO:YG2.onDefaultSaves -= InitializeSDK;
    }
}
