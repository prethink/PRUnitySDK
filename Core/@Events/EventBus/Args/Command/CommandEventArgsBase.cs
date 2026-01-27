public class CommandEventArgsBase : EventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Command");
    }
}
