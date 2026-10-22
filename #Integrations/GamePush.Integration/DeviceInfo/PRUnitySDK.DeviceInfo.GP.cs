#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(DeviceInfoBase), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeDeviceInfoOverrideGP()
    {
        DeviceInfo = new GamePushDeviceInfo();
    }
}
#endif
