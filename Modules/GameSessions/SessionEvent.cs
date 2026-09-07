namespace PRGameSessions
{
    /// <summary>
    /// Состояние фиксируется до публикации; PlayerId заполнен для входа и выхода участника.
    /// </summary>
    public sealed class SessionEvent
    {
        public SessionEventKind Kind { get; }
        public SessionSnapshot Snapshot { get; }
        public long? PlayerId { get; }

        internal SessionEvent(SessionEventKind kind, SessionSnapshot snapshot, long? playerId = null)
        {
            Kind = kind;
            Snapshot = snapshot;
            PlayerId = playerId;
        }
    }
}
