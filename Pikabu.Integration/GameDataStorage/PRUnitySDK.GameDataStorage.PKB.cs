#if !PRSDK_DISABLE_PIKABU
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IGameDataStorage), PrioritySDK.OVERRIDE_PROPERTY_PKB_PRIORITY)]
    private static void InitializeGameDataStorageOverridePKB()
    {
        GameDataStorage = new PikabuGameDataStorager();
    }
}
#endif
