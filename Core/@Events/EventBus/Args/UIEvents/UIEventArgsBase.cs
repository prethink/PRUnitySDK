public abstract class UIEventArgsBase : EventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "UI");
    }
}
