#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(PlayerNameServiceBase), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializePlayerNameServiceOverrideGP()
    {
        CurrentPlayerName = new GamePushPlayerNameService();
    }
}
#endif
