#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ILanguageManager), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void OverrideTranslateGP()
    {
        LanguageManager = new GamePushLanguageManager();
    }
}
#endif
