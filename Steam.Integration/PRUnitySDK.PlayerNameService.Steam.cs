#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(PlayerNameServiceBase), PrioritySDK.OVERRIDE_PROPERTY_STEAM_PRIORITY)]
    private static void InitializePlayerNameServiceOverrideSteam()
    {
        CurrentPlayerName = new SteamPlayerNameService();
    }
}
#endif
#endif
