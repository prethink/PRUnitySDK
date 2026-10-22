#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IGameDataStorage), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeGameDataStorageOverrideGP()
    {
        GameDataStorage = new GamePushGameDataStorager();
    }
}
#endif
