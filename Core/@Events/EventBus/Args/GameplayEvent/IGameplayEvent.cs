public interface IGameplayEvent : IGlobalSubscriber
{
    void Track(GameplayEventArgsBase args);
}
