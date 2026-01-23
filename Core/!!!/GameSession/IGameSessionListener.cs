public interface IGameSessionListener 
{
    public GameSessionBehaviour GameSessionBehaviour { get; }

    public void OnPreparingData();

    public void OnSessionStart();

    public void OnSessionEnd();
}
