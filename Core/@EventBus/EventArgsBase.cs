using System;

/// <summary>
/// Базовые аргументы события.
/// </summary>
public abstract class EventArgsBase
{
    /// <summary>
    /// Идентификатор события.
    /// </summary>
    public abstract string EventId { get; }

    /// <summary>
    /// Время события.
    /// </summary>
    public virtual DateTime EventTime { get; }
}
