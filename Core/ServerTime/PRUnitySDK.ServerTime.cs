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

            // Часы таймеров идут вперёд вместе с ускорением времени для проверки (PRDebugTime); в релизе обёртки нет.
            if (PRDebugTime.Allowed && ServerTime != null && ServerTime is not PRDebugServerTime)
                ServerTime = new PRDebugServerTime(ServerTime);

            return ServerTime;
        });
    }

    #endregion
}
