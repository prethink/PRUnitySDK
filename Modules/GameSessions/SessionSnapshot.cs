using System;
using System.Collections.Generic;

namespace PRGameSessions
{
    /// <summary>
    /// Неизменяемый снимок для поздних подписчиков и отложенной обработки событий.
    /// </summary>
    public sealed class SessionSnapshot
    {
        public Guid SessionId { get; }
        public string MapKey { get; }
        public SessionState State { get; }
        public Guid RoundId { get; }
        public int RoundNumber { get; }
        public string ModeKey { get; }
        public RoundPhase Phase { get; }
        public RoundResult Result { get; }
        public SessionEndReason? EndReason { get; }
        public IReadOnlyList<SessionParticipant> Participants { get; }

        internal SessionSnapshot(Guid sessionId, string mapKey, SessionState state,
            Guid roundId, int roundNumber, string modeKey, RoundPhase phase,
            RoundResult result, SessionEndReason? endReason, IEnumerable<SessionParticipant> participants)
        {
            SessionId = sessionId;
            MapKey = mapKey;
            State = state;
            RoundId = roundId;
            RoundNumber = roundNumber;
            ModeKey = modeKey;
            Phase = phase;
            Result = result;
            EndReason = endReason;
            Participants = new List<SessionParticipant>(participants).AsReadOnly();
        }
    }
}
