public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_DEVICE_INFO = 30;

    /// <summary>
    /// Информация об устройстве.   
    /// </summary>
    public static DeviceInfoBase DeviceInfo;

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_DEVICE_INFO)]
    private static void InitializeDeviceInfo()
    {
        InitializeModuleSDK(nameof(DeviceInfoBase), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(DeviceInfoBase));

            InitializeDefault(nameof(DeviceInfo), () => DeviceInfo, () => 
            { 
                DeviceInfo = new LocalDeviceInfo(); return DeviceInfo; 
            });

            return DeviceInfo;
        });
    }
}
