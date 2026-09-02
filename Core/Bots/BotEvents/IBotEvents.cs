public interface IBotStopEvent : IGlobalSubscriber
{
    void StopEvent(BotStopEventArgs args);
}

public interface IBotStartEvent : IGlobalSubscriber
{
    void StartEvent(BotStartEventArgs args);
}

public interface IBotSetTargetEvent : IGlobalSubscriber
{
    void SetPathEvent(BotSetTargetEventArgs args);
}