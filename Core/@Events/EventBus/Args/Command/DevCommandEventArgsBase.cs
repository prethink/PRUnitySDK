public class DevCommandEventArgsBase : CommandEventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Dev");
    }
}
