public class EntitiesEventArgsBase : EventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "EntitiesEvent");
    }
}
