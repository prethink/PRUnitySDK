public interface IBotStopEvent : IGlobalSubscriber
{
    void StopEvent(BotStopEventArgs args);
}

public interface IBotStartEvent : IGlobalSubscriber
{
    void StartEvent(BotStartEventArgs args);
}

public interface IBotSetPathEvent : IGlobalSubscriber
{
    void SetPathEvent(BotSetPathEventArgs args);
}