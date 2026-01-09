public sealed partial class PRUnitySDK
{
    /// <summary>
    /// Настройки.
    /// </summary>
    public readonly static PRSettingsContainer Settings = new();

    /// <summary>
    /// База данных.
    /// </summary>
    public readonly static PRDatabaseContainer Database = new();

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
