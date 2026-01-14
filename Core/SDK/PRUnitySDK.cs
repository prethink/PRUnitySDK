public sealed partial class PRUnitySDK
{
    /// <summary>
    /// Настройки.
    /// </summary>
    public static PRSDKSettings Settings => PRSDKSettings.Instance;

    /// <summary>
    /// База данных.
    /// </summary>
    public static PRSDKDatabase Database => PRSDKDatabase.Instance;

    /// <summary>
    /// Менеджеры.
    /// </summary>
    public readonly static PRManagerContainer Managers = new();

    /// <summary>
    /// Сервис управления паузой.
    /// </summary>
    public static IPauseManager PauseManager => PauseProvider.Instance;

    /// <summary>
    /// Менеджер наблюдателей.
    /// </summary>
    public static WatcherManager WatcherManager => WatcherProvider.Instance;
}
