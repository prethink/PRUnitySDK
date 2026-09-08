public class EntityEquipmentChangedEventArgs : EntityEventArgsBase
{
    public EntityEquipmentChangedEventArgs(IEntity entity) : base(entity)
    {
    }
}

public interface IEntityEquipmentChangedEvent : IGlobalSubscriber
{
    public void OnEntityEquipmentChanged(EntityEquipmentChangedEventArgs args);
}
