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
    /// <returns><see langword="true"/>, если окно найдено и отображено.</returns>
    public bool TryShowWindow(string key);

    /// <summary>
    /// Попытаться отобразить окно по ключу.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="args">Параметры для окна.</param>
    /// <returns><see langword="true"/>, если окно найдено и отображено.</returns>
    public bool TryShowWindow(string key, MonoWindowArgs args);
}
