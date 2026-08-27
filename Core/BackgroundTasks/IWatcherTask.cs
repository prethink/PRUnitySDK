/// <summary>
/// Контракт задачи-наблюдателя: опрашивает значение и сообщает об его изменении.
/// </summary>
/// <typeparam name="T">Тип наблюдаемого значения.</typeparam>
/// <remarks>
/// Реализуется и обычным классом (<see cref="WatcherTask{T}"/>), и компонентом сцены
/// (<see cref="WatcherTaskBehaviour{T}"/>). Общая логика сравнения и уведомления живёт
/// в <see cref="WatcherState{T}"/> - тем же мостом, что и расписание в
/// <see cref="BackgroundTaskRuntime"/>.
/// </remarks>
public interface IWatcherTask<T> : IBackgroundTask
{
    /// <summary>
    /// Наблюдаемое значение и его события.
    /// </summary>
    WatcherState<T> Watcher { get; }

    /// <summary>
    /// Поднимать событие изменения при самом первом чтении.
    /// </summary>
    bool RaiseOnFirstRead { get; }

    /// <summary>
    /// Читает текущее значение наблюдаемого источника.
    /// </summary>
    T Read();

    /// <summary>
    /// Сравнивает два значения.
    /// </summary>
    bool AreValuesEqual(T left, T right);
}
