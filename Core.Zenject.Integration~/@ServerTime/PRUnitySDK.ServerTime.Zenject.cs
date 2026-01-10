public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ServerTimeBase), PrioritySDK.OVERRIDE_PROPERTY_ZINJECTION_PRIORITY)]
    private static void InitializeServerTimeOverride()
    {
        TryResolveModuleZInject<ServerTimeBase>(() => ServerTime, (setter) => ServerTime = setter);
    }
}
