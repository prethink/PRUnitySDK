using System;

namespace PRGameSessions
{
    public interface IGameSessionsService : IReadySignalProvider
    {
        bool Enabled { get; }
        Guid SessionId { get; }
        SessionSnapshot Snapshot { get; }
        SessionScope SessionEntities { get; }
        RoundContext CurrentRound { get; }
        bool SetEnabled(bool enabled);
        bool TryStartSession(string mapKey);
        bool TryEndSession(Guid sessionId, SessionEndReason reason = SessionEndReason.Completed);
        bool TryPrepareRound(RoundPlan plan);
        bool TryStartRound(Guid roundId);
        bool TryEndRound(Guid roundId, RoundResult result);
        bool TryJoin(SessionParticipant participant);
        bool TryLeave(long playerId);
        void Tick(float deltaTime);
    }
}
