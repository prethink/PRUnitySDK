public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_SERVER_GAME_DATA_STORAGE = 20;

    /// <summary>
    /// Серверное время.
    /// </summary>
    public static IGameDataStorage GameDataStorage;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_SERVER_GAME_DATA_STORAGE)]
    private static void InitializeGameDataStorage()
    {
        InitializeModuleSDK(nameof(IGameDataStorage), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IGameDataStorage));

            InitializeDefault(nameof(GameDataStorage), () => GameDataStorage, () => { GameDataStorage = new PlayerPrefsSaveLoadManager(); return GameDataStorage; });

            return GameDataStorage;
        });
    }

    #endregion
}
