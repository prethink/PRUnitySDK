#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ILanguageManager), PrioritySDK.OVERRIDE_PROPERTY_STEAM_PRIORITY)]
    private static void OverrideTranslateSteam()
    {
        LanguageManager = new SteamLanguageManager();
    }
}
#endif
#endif
