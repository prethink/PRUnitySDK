public sealed partial class PRUnitySDK 
{
    /// <summary>
    /// Игровые настройки.
    /// </summary>
    public static PRGameSettings GameSettings { get; private set; }

    /// <summary>
    /// База данных.
    /// </summary>
    public static PRDatabase Database { get; private set; }

    /// <summary>
    /// Сервис управления паузой.
    /// </summary>
    public static IPauseManager PauseManager => PauseProvider.Instance;

    /// <summary>
    /// Менеджер наблюдателей.
    /// </summary>
    public static WatcherManager WatcherManager => WatcherProvider.Instance;
}
