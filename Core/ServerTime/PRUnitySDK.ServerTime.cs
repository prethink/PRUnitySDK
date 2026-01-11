public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_SERVER_TIME = 10;

    /// <summary>
    /// Серверное время.
    /// </summary>
    public static IServerTime ServerTime;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_SERVER_TIME)]
    private static void InitializeServerTime()
    {
        InitializeModuleSDK(nameof(IServerTime), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IServerTime));

            InitializeDefault(nameof(IServerTime), () => ServerTime, () => { ServerTime = new LocalServerTime(); return ServerTime; });

            return ServerTime;
        });
    }

    #endregion
}
