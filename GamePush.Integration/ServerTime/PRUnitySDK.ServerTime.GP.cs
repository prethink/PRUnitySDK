#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IServerTime), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeServerTimeOverrideGP()
    {
        ServerTime = new GamePushServerTime();
    }
}
#endif
