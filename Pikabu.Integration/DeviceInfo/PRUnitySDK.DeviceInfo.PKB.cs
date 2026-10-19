#if !PRSDK_DISABLE_PIKABU
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(DeviceInfoBase), PrioritySDK.OVERRIDE_PROPERTY_PKB_PRIORITY)]
    private static void InitializeDeviceInfoOverridePKB()
    {
        DeviceInfo = new PikabuDeviceInfo();
    }
}
#endif
