public abstract class GameplayEventArgsBase : EventArgsBase
{
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Gameplay");
    }
}
