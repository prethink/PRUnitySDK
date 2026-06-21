public class EntityEventArgsBase : EventArgsBase
{
    public IEntity Entity { get; protected set; }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "EntityEvent");
    }

    public EntityEventArgsBase(IEntity entity) : base()
    {
        this.Entity = entity;
    }
}
