using System;

/// <summary>
/// Базовые аргументы события.
/// </summary>
public abstract class EventArgsBase
{
    /// <summary>
    /// Время события.
    /// </summary>
    public virtual DateTime EventTime { get; protected set; }

    /// <summary>
    /// Получить EventId.
    /// </summary>
    /// <returns>EventId.</returns>
    public virtual CategoryPath GetEventId()
    {
        return new CategoryPath("Event");
    }

    protected EventArgsBase()
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
    }
}
