using System;

namespace PRGameSessions
{
    /// <summary>
    /// Запрос завершения привязан к RoundId, поэтому старый callback не завершит новый раунд.
    /// </summary>
    public sealed class RoundContext
    {
        private readonly GameSessionsController controller;
        public Guid SessionId { get; }
        public Guid RoundId { get; }
        public int Number { get; }
        public SessionScope Entities { get; }
        public SessionSnapshot Snapshot => controller.Snapshot;

        internal RoundContext(GameSessionsController controller, Guid sessionId, int number, SessionScope scope)
        {
            this.controller = controller;
            SessionId = sessionId;
            RoundId = scope.Id;
            Number = number;
            Entities = scope;
        }

        public bool TryEnd(RoundResult result) => controller.TryEndRound(RoundId, result);
    }
}
