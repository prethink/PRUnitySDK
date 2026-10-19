#if !PRSDK_DISABLE_VK
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(ILanguageManager), PrioritySDK.OVERRIDE_PROPERTY_VK_PRIORITY)]
    private static void OverrideTranslateVK()
    {
        LanguageManager = new VKLanguageManager();
    }
}
#endif
