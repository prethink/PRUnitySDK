/// <summary>
/// Интерфейс слушателя паузы.
/// Реагирует на изменения состояния паузы.
/// </summary>
public interface IPauseStateListener : IGlobalSubscriber
{
    /// <summary>
    /// Событие изменения состояния паузы.
    /// </summary>
    /// <param name="args">Аргументы события.</param>
    void OnPauseStateChanged(PauseEventArgs args);
}
