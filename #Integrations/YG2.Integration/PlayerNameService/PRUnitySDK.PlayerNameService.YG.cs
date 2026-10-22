#if !PRSDK_DISABLE_YG2
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(PlayerNameServiceBase), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializePlayerNameServiceOverrideYG()
    {
        CurrentPlayerName = new YandexPlayerNameService();
    }
}
#endif
