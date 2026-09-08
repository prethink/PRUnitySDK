public interface IEntitiesEvent : IGlobalSubscriber
{
    void Track(EntitiesEventArgsBase args);
}