public partial class GameplayEvents
{
    /// <summary>
    /// Поднимает игровой сигнал.
    /// </summary>
    /// <param name="signal">Сигнал из <see cref="GameSignalEnumerations"/>.</param>
    /// <param name="source">Кто его вызвал, если это важно слушателю.</param>
    /// <param name="count">Сколько раз случилось разом: продано три питомца — 3.</param>
    /// <remarks>
    /// Это единственный вызов, которым игра сообщает о своём событии: его слышат обучение,
    /// достижения и ежедневные цели (через слой действий игрока). Звать их сервисы отдельно не нужно.
    /// </remarks>
    public static void RaiseSignal(Enumeration signal, IEntity source = null, long count = 1)
    {
        if (signal == null || count <= 0)
            return;

        var args = new GameSignalEventArgs(signal, source, count);
        EventBus.RaiseEvent<IGameplayEvent>(invoke => invoke.Track(args));
    }
}
