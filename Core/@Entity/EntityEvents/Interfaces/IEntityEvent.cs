public interface IEntityEvent : IGlobalSubscriber
{
    void Track(EntityEventArgsBase args);
}