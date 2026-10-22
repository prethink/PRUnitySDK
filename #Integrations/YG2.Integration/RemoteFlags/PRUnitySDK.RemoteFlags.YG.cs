#if !PRSDK_DISABLE_YG2
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IRemoteFlags), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeRemoteFlagsOverrideYG()
    {
        RemoteFlags = new YandexRemoteFlags();
    }
}
#endif
