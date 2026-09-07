namespace PRGameSessions
{
    /// <summary>
    /// Участник сессии переживает замену персонажа между раундами.
    /// </summary>
    public sealed class SessionParticipant
    {
        public long PlayerId { get; }
        public string TeamId { get; }

        public SessionParticipant(long playerId, string teamId = null)
        {
            PlayerId = playerId;
            TeamId = teamId;
        }
    }
}
