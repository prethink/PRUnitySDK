/// <summary>
/// Игровой сигнал: случилось событие из <see cref="GameSignalEnumerations"/>.
/// </summary>
/// <remarks>
/// Для случаев, когда слушателю важен сам факт, а не подробности: шаг обучения ждёт
/// «открыт лаки-блок», достижение считает такие события. Своё событие на каждый такой
/// факт заводить не нужно.
/// </remarks>
public class GameSignalEventArgs : GameplayEventArgsBase
{
    /// <summary>
    /// Какой сигнал.
    /// </summary>
    public Enumeration Signal { get; }

    /// <summary>
    /// Кто вызвал сигнал, если это важно, — например, открывший игрок. Может быть пусто.
    /// </summary>
    public IEntity Source { get; }

    public GameSignalEventArgs(Enumeration signal, IEntity source = null)
    {
        Signal = signal;
        Source = source;
    }

    /// <inheritdoc />
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Signal");
    }
}
