public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_SERVER_TIME = 10;

    /// <summary>
    /// Серверное время.
    /// </summary>
    public static ServerTimeBase ServerTime;

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_SERVER_TIME)]
    private static void InitializeServerTime()
    {
        InitializeModuleSDK(nameof(ServerTime), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(ServerTimeBase));

            InitializeDefault(nameof(ServerTime), () => ServerTime, () => { ServerTime = new LocalServerTime(); return ServerTime; });

            return ServerTime;
        });
    }

    #endregion
}
