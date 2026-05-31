/// <summary>
/// Глобальные события MonoWindow.
/// </summary>
public interface IMonoWindowEvents : IGlobalSubscriber
{
    /// <summary>
    /// Скрыть все открытые окна.
    /// </summary>
    public void HideAllWindows();

    /// <summary>
    /// Скрыть принудительно все окна.
    /// </summary>
    public void HideForceAllWindows();

    /// <summary>
    /// Попытаться отобразить окно по ключу.
    /// </summary>
    /// <param name="key">Ключ.</param>
    public void TryShowWindow(string key);

    /// <summary>
    /// Попытаться отобразить окно по ключу.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="args">Параметры для окна.</param>
    public void TryShowWindow(string key, MonoWindowArgs args);
}
