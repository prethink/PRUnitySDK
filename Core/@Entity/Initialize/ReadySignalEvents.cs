public static class ReadySignalEvents 
{
    public static void RaiseReadySignal(string name) => EventBus.RaiseEvent<IReadySignalEvent>(x => x.OnReadySignal(name));
}

public interface IReadySignalEvent : IGlobalSubscriber
{
    void OnReadySignal(string name);
}