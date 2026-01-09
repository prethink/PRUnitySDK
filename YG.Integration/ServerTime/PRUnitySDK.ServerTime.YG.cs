public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ServerTimeBase), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeServerTimeOverrideYG()
    {
        ServerTime = new YandexServerTime();
    }
}
