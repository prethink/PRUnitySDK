/// <summary>
/// Состояние фоновой задачи в цикле выполнения.
/// </summary>
public enum BackgroundTaskStatus
{
    /// <summary>
    /// Создана, но ещё не зарегистрирована в трекере.
    /// </summary>
    Pending,

    /// <summary>
    /// Ожидает наступления времени первого запуска.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Выполняется прямо сейчас.
    /// </summary>
    Executing,

    /// <summary>
    /// Ожидает следующего запуска по расписанию.
    /// </summary>
    WaitingNextRun,

    /// <summary>
    /// Последний запуск был пропущен, потому что <c>CanExecute()</c> вернул false.
    /// </summary>
    Skipped,

    /// <summary>
    /// Приостановлена вручную и не выполняется до возобновления.
    /// </summary>
    Paused,

    /// <summary>
    /// Отключена из-за череды ошибок подряд.
    /// </summary>
    Faulted,

    /// <summary>
    /// Исчерпала заданное количество запусков и больше не выполняется.
    /// </summary>
    Completed
}
