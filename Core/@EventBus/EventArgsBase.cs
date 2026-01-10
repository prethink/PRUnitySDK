using System;

public abstract class EventArgsBase
{
    public abstract string EventId { get; }
    public abstract DateTime EventTime { get; }
}
