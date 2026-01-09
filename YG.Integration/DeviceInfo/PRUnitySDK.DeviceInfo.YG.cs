public partial class PRUnitySDK
{
    [OverrideProperty(typeof(DeviceInfoBase), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeDeviceInfoOverrideYG()
    {
        DeviceInfo = new YandexDeviceInfo();
    }
}
