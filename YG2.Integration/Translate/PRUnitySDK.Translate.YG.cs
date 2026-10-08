#if !PRSDK_DISABLE_YG2
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ILanguageManager), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void OverrideTranslateYG()
    {
        LanguageManager = new YGLanguageManager();
    }
}
#endif
