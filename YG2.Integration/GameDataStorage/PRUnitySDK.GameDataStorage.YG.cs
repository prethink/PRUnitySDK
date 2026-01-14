public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IGameDataStorage), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeGameDataStorageOverrideYG()
    {
        GameDataStorage = new YandexGameDataStorager();
    }
}
