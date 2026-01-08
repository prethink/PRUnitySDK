using YG;

public partial class Bootstrap
{
    [OverrideBootstrap]
    public void OverrideInitialize()
    {
        isOverriden = true;
    }

    private void OnEnable()
    {
        YG2.onGetSDKData += InitializeSDK;
        YG2.onDefaultSaves += InitializeSDK;
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= InitializeSDK;
        YG2.onDefaultSaves -= InitializeSDK;
    }
}
