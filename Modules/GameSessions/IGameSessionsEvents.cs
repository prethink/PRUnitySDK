namespace PRGameSessions
{
    public interface IGameSessionsEvents : IGlobalSubscriber
    {
        void OnSessionEvent(SessionEvent notification);
    }
}
