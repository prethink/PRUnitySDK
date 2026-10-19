#if !PRSDK_DISABLE_PIKABU
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ILanguageManager), PrioritySDK.OVERRIDE_PROPERTY_PKB_PRIORITY)]
    private static void OverrideTranslatePKB()
    {
        LanguageManager = new PikabuLanguageManager();
    }
}
#endif
