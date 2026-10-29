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

    /// <summary>
    /// Сколько раз случилось: «продано три питомца» — один сигнал со счётом 3. Не меньше единицы.
    /// </summary>
    /// <remarks>
    /// Шагу обучения важен сам факт, счётчикам достижений и ежедневных целей — количество.
    /// </remarks>
    public long Count { get; }

    public GameSignalEventArgs(Enumeration signal, IEntity source = null, long count = 1)
    {
        Signal = signal;
        Source = source;
        Count = count < 1 ? 1 : count;
    }

    /// <inheritdoc />
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Signal");
    }
}
