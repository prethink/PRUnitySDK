public partial class GameplayEvents
{
    /// <summary>
    /// Поднимает игровой сигнал.
    /// </summary>
    /// <param name="signal">Сигнал из <see cref="GameSignalEnumerations"/>.</param>
    /// <param name="source">Кто его вызвал, если это важно слушателю.</param>
    public static void RaiseSignal(Enumeration signal, IEntity source = null)
    {
        if (signal == null)
            return;

        var args = new GameSignalEventArgs(signal, source);
        EventBus.RaiseEvent<IGameplayEvent>(invoke => invoke.Track(args));
    }
}
