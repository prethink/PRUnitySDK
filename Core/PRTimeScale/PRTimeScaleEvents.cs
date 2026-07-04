public interface IOnPRTimeScaleChange : IGlobalSubscriber
{
    void OnPRTimeScaleChange(Enumeration layer, float value);
}

public static class PRTimeScaleEvents
{
    public static void RaiseTimeScaleChange(Enumeration enumeration, float value) 
        => EventBus.RaiseEvent<IOnPRTimeScaleChange>(x => x.OnPRTimeScaleChange(enumeration, value));
}