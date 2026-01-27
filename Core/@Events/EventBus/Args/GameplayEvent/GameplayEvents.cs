/// <summary>
/// События игрового процесса.
/// </summary>
public partial class GameplayEvents
{
    /// <summary>
    /// Вызвать событие сохранения игры.
    /// </summary>
    public static void RaiseSaveEvent() => EventBus.RaiseEvent<IGameplayEvent>(invoke => invoke.Track(new SaveGameEventArgs()));

    /// <summary>
    /// Вызвать событие готовности игры.
    /// </summary>
    public static void RaiseGameReady() => EventBus.RaiseEvent<IReadyGameEvent>(invoke => invoke.OnReadyGame());
}
