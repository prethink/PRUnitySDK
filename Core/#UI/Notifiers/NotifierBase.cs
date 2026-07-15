public abstract class NotifierBase : PRMonoBehaviour
{
    public abstract Enumeration Key { get; }

    protected override void RegisterEventsOnCreated()
    {
        PRUnitySDK.Trackers.Notifiers.Register(this);
        base.RegisterEventsOnCreated();
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        PRUnitySDK.Trackers.Notifiers.Unregister(this);
        base.UnRegisterEventsOnDestroy();
    }
}
