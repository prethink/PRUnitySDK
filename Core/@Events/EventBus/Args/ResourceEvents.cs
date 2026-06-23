
public static class ResourceEvents
{
    public static void RaiseResourceUpdate(ResourceEventArgs args) => EventBus.RaiseEvent<IResourceEvent>(x => x.OnResourceUpdate(args));
    public static void RaiseResourceValueChange(ResourceValueChangeEventArgs args) => EventBus.RaiseEvent<IResourceEvent>(x => x.OnResourceUpdate(args));
}

public interface IResourceEvent : IGlobalSubscriber
{
    void OnResourceUpdate(ResourceEventArgs args);
}

public class ResourceEventArgs : EventArgsBase
{
    public Enumeration ResourceType { get; protected set; }

    public ResourceEventArgs(Enumeration resourceType) : base()
    {
        this.ResourceType = resourceType;
    }
}

public class ResourceValueChangeEventArgs : ResourceEventArgs
{
    public long Value { get; protected set; }

    public ResourceValueChangeEventArgs(Enumeration resourceType, long value) : base(resourceType)
    {
        this.Value = value;
    }
}