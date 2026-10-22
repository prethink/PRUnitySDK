#if !PRSDK_DISABLE_VK
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(DeviceInfoBase), PrioritySDK.OVERRIDE_PROPERTY_VK_PRIORITY)]
    private static void InitializeDeviceInfoOverrideVK()
    {
        DeviceInfo = new VKDeviceInfo();
    }
}
#endif
