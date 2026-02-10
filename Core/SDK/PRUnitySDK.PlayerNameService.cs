public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_PLAYER_NAME_SERVICE = 60;

    /// <summary>
    /// Игровой никнейм текущего игрока.
    /// </summary>
    public static PlayerNameServiceBase CurrentPlayerName;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_PLAYER_NAME_SERVICE)]
    private static void InitializePlayerNameService()
    {
        InitializeModuleSDK(nameof(PlayerNameServiceBase), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(PlayerNameServiceBase));

            InitializeDefault(nameof(PlayerNameServiceBase), () => CurrentPlayerName, () => { CurrentPlayerName = new LocalPlayerNameService(); return CurrentPlayerName; });

            return CurrentPlayerName;
        });
    }

    #endregion
}
