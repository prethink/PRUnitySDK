#if !PRSDK_DISABLE_PIKABU
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(PlayerNameServiceBase), PrioritySDK.OVERRIDE_PROPERTY_PKB_PRIORITY)]
    private static void InitializePlayerNameServiceOverridePKB()
    {
        CurrentPlayerName = new PikabuPlayerNameService();
    }
}
#endif
