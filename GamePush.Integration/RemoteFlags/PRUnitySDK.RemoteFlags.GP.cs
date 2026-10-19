#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IRemoteFlags), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeRemoteFlagsOverrideGP()
    {
        RemoteFlags = new GamePushRemoteFlags();
    }
}
#endif
