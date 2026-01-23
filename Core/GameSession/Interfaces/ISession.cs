using System;

public interface ISession
{
    Guid SessionId { get; }
    DateTime StartTime { get; }
    DateTime? EndTime { get; }

    void Start();
    void End();
}