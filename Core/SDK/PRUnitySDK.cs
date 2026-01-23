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
    /// Утилиты.
    /// </summary>
    public static PRUtils Utils => PRUtils.Instance;

    /// <summary>
    /// Трекеры.
    /// </summary>
    public static PRTrackers Trackers => new();

    /// <summary>
    /// Менеджеры.
    /// </summary>
    public readonly static PRManagerContainer Managers = new();

    /// <summary>
    /// Сервис управления паузой.
    /// </summary>
    public static IPauseManager PauseManager => PauseProvider.Instance;
}
