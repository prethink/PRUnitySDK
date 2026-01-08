public partial class PRUnitySDK
{
    /// <summary>
    /// Серверное время.
    /// </summary>
    public static ServerTimeBase ServerTime;

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [InitializeMethod(InitializeType.SDK, 1)]
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
