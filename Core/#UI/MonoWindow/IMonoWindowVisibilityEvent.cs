/// <summary>
/// Окно показано или скрыто.
/// </summary>
/// <remarks>
/// Поднимает <see cref="MonoWindowsTracker"/>. «Скрыто» приходит только для окна, о показе
/// которого уже сообщалось: выключение никогда не открытого окна событием не считается.
/// </remarks>
public interface IMonoWindowVisibilityEvent : IGlobalSubscriber
{
    /// <summary>
    /// Окно показано.
    /// </summary>
    void OnWindowShown(MonoWindowBase window);

    /// <summary>
    /// Окно скрыто.
    /// </summary>
    void OnWindowHidden(MonoWindowBase window);
}
