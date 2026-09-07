namespace PRGameSessions
{
    /// <summary>
    /// Результат содержит идентификаторы участников, а не ссылки на уничтожаемые персонажи.
    /// </summary>
    public sealed class RoundResult
    {
        public SessionEndReason Reason { get; }
        public long? WinnerPlayerId { get; }
        public string WinnerTeamId { get; }
        public bool IsDraw => (Reason == SessionEndReason.Completed || Reason == SessionEndReason.TimeLimit)
            && !WinnerPlayerId.HasValue && WinnerTeamId == null;

        public RoundResult(SessionEndReason reason = SessionEndReason.Completed,
            long? winnerPlayerId = null, string winnerTeamId = null)
        {
            Reason = reason;
            WinnerPlayerId = winnerPlayerId;
            WinnerTeamId = winnerTeamId;
        }
    }
}
