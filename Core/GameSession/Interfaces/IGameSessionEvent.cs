public interface IGameSessionEvent : IGlobalSubscriber
{
    void Track(GameSessionEventArgsBase args);
}
