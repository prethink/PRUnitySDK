#if !PRSDK_DISABLE_VK
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IGameDataStorage), PrioritySDK.OVERRIDE_PROPERTY_VK_PRIORITY)]
    private static void InitializeGameDataStorageOverrideVK()
    {
        GameDataStorage = new VKGameDataStorager();
    }
}
#endif
