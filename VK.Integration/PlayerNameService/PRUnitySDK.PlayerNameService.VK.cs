#if !PRSDK_DISABLE_VK
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(PlayerNameServiceBase), PrioritySDK.OVERRIDE_PROPERTY_VK_PRIORITY)]
    private static void InitializePlayerNameServiceOverrideVK()
    {
        CurrentPlayerName = new VKPlayerNameService();
    }
}
#endif
