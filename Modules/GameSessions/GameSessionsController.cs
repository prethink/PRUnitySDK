using System;
using System.Collections.Generic;

namespace PRGameSessions
{
    /// <summary>
    /// Переходы выполняются на главном потоке. Во время уведомления вложенные переходы отклоняются.
    /// </summary>
    public sealed class GameSessionsController : IGameSessionsService
    {
        private readonly Action<SessionEvent> publish;
        private readonly Action<Exception> reportError;
        private readonly ReadySignal ready = new ReadySignal(nameof(GameSessionsController));
        private readonly Dictionary<long, SessionParticipant> participants = new();
        private readonly List<RoundBehaviour> behaviours = new();
        private bool busy;
        private Guid sessionId;
        private string mapKey;
        private SessionState state;
        private RoundPhase phase;
        private int roundNumber;
        private string modeKey;
        private RoundResult result;
        private RoundResult pendingResult;
        private SessionEndReason? endReason;
        private SessionEndReason? pendingSessionEnd;

        public bool Enabled { get; private set; }
        public Guid SessionId => sessionId;
        public IReadySignal ReadySignal => ready;
        public SessionScope SessionEntities { get; private set; }
        public RoundContext CurrentRound { get; private set; }
        public SessionSnapshot Snapshot => new SessionSnapshot(sessionId, mapKey, state,
            CurrentRound?.RoundId ?? Guid.Empty, roundNumber, modeKey, phase, result, endReason, participants.Values);

        public GameSessionsController(Action<SessionEvent> publish = null, Action<Exception> reportError = null)
        {
            this.publish = publish ?? (message => EventBus.RaiseEvent<IGameSessionsEvents>(x => x.OnSessionEvent(message)));
            this.reportError = exception =>
            {
                try { (reportError ?? UnityEngine.Debug.LogException)(exception); }
                catch { /* Diagnostics must not interrupt cleanup. */ }
            };
            ready.SetReady();
        }

        public bool SetEnabled(bool enabled)
        {
            if (busy) return false;
            if (Enabled == enabled) return true;
            busy = true;
            try
            {
                if (!enabled && state == SessionState.Active) EndSession(SessionEndReason.Disabled);
                Enabled = enabled;
                return true;
            }
            finally { DrainRoundEnd(); busy = false; }
        }

        public bool TryStartSession(string key)
        {
            if (!Enabled || busy || state == SessionState.Active || state == SessionState.Ending) return false;
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Map key is required.", nameof(key));
            busy = true;
            try
            {
                SessionEntities = new SessionScope(reportError);
                sessionId = SessionEntities.Id;
                mapKey = key;
                participants.Clear();
                CurrentRound = null;
                roundNumber = 0;
                modeKey = null;
                phase = RoundPhase.None;
                result = pendingResult = null;
                endReason = null;
                state = SessionState.Active;
                Notify(SessionEventKind.SessionStarted);
                return true;
            }
            finally { DrainRoundEnd(); busy = false; }
        }

        public bool TryPrepareRound(RoundPlan plan)
        {
            if (!Enabled || busy || state != SessionState.Active ||
                (phase != RoundPhase.None && phase != RoundPhase.Ended)) return false;
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            busy = true;
            bool success = true;
            try
            {
                CurrentRound = new RoundContext(this, sessionId, ++roundNumber, new SessionScope(reportError));
                modeKey = plan.ModeKey;
                result = pendingResult = null;
                phase = RoundPhase.Preparing;
                Notify(SessionEventKind.RoundPreparing);
                foreach (var factory in plan.Factories)
                {
                    if (pendingResult != null) break;
                    var behaviour = factory() ?? throw new InvalidOperationException("Round factory returned null.");
                    behaviours.Add(behaviour);
                    behaviour.Prepare(CurrentRound);
                }
            }
            catch (Exception exception)
            {
                reportError(exception);
                pendingResult = new RoundResult(SessionEndReason.Error);
                success = false;
            }
            finally { DrainRoundEnd(); busy = false; }
            return success;
        }

        public bool TryStartRound(Guid roundId)
        {
            if (busy || state != SessionState.Active || phase != RoundPhase.Preparing || CurrentRound.RoundId != roundId) return false;
            busy = true;
            bool success = true;
            try
            {
                phase = RoundPhase.Playing;
                foreach (var behaviour in behaviours)
                {
                    if (pendingResult != null) break;
                    behaviour.Start(CurrentRound);
                }
                Notify(SessionEventKind.RoundStarted);
            }
            catch (Exception exception)
            {
                reportError(exception);
                pendingResult = new RoundResult(SessionEndReason.Error);
                success = false;
            }
            finally { DrainRoundEnd(); busy = false; }
            return success;
        }

        /// <summary>
        /// Первый результат побеждает. Внутри callback завершение откладывается до выхода из него.
        /// </summary>
        public bool TryEndRound(Guid roundId, RoundResult roundResult)
        {
            if (CurrentRound == null || CurrentRound.RoundId != roundId || state != SessionState.Active ||
                (phase != RoundPhase.Preparing && phase != RoundPhase.Playing) || pendingResult != null) return false;
            pendingResult = roundResult ?? throw new ArgumentNullException(nameof(roundResult));
            if (!busy)
            {
                busy = true;
                try { DrainRoundEnd(); }
                finally { busy = false; }
            }
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (busy || !Enabled || state != SessionState.Active || phase != RoundPhase.Playing) return;
            busy = true;
            try
            {
                foreach (var behaviour in behaviours)
                {
                    if (pendingResult != null) break;
                    behaviour.Tick(CurrentRound, deltaTime);
                }
            }
            catch (Exception exception)
            {
                reportError(exception);
                pendingResult = new RoundResult(SessionEndReason.Error);
            }
            finally { DrainRoundEnd(); busy = false; }
        }

        public bool TryEndSession(Guid expectedSessionId, SessionEndReason reason = SessionEndReason.Completed)
        {
            if (state != SessionState.Active || sessionId != expectedSessionId || pendingSessionEnd.HasValue) return false;
            if (busy)
            {
                pendingSessionEnd = reason;
                return true;
            }
            busy = true;
            try { EndSession(reason); return true; }
            finally { busy = false; }
        }

        private void EndSession(SessionEndReason reason)
        {
            state = SessionState.Ending;
            endReason = reason;
            Notify(SessionEventKind.SessionEnding);
            if (phase == RoundPhase.Preparing || phase == RoundPhase.Playing)
                EndRound(new RoundResult(reason == SessionEndReason.Completed ? SessionEndReason.Cancelled : reason));
            SessionEntities.Dispose();
            state = SessionState.Ended;
            Notify(SessionEventKind.SessionEnded);
        }

        private void DrainRoundEnd()
        {
            if (pendingResult != null)
            {
                var requested = pendingResult;
                pendingResult = null;
                EndRound(requested);
            }
            if (pendingSessionEnd.HasValue)
            {
                var reason = pendingSessionEnd.Value;
                pendingSessionEnd = null;
                if (state == SessionState.Active) EndSession(reason);
            }
        }

        private void EndRound(RoundResult roundResult)
        {
            result = roundResult;
            phase = RoundPhase.Ending;
            Notify(SessionEventKind.RoundEnding);
            for (int i = behaviours.Count - 1; i >= 0; i--)
            {
                try { behaviours[i].Stop(CurrentRound, result); }
                catch (Exception exception) { reportError(exception); }
            }
            behaviours.Clear();
            CurrentRound.Entities.Dispose();
            phase = RoundPhase.Ended;
            Notify(SessionEventKind.RoundEnded);
        }

        public bool TryJoin(SessionParticipant participant)
        {
            if (participant == null) throw new ArgumentNullException(nameof(participant));
            if (busy || state != SessionState.Active || participants.ContainsKey(participant.PlayerId)) return false;
            busy = true;
            try
            {
                participants.Add(participant.PlayerId, participant);
                Notify(SessionEventKind.ParticipantJoined, participant.PlayerId);
                return true;
            }
            finally { DrainRoundEnd(); busy = false; }
        }

        public bool TryLeave(long playerId)
        {
            if (busy || state != SessionState.Active || !participants.ContainsKey(playerId)) return false;
            busy = true;
            try
            {
                participants.Remove(playerId);
                Notify(SessionEventKind.ParticipantLeft, playerId);
                return true;
            }
            finally { DrainRoundEnd(); busy = false; }
        }

        private void Notify(SessionEventKind kind, long? playerId = null)
        {
            try { publish(new SessionEvent(kind, Snapshot, playerId)); }
            catch (Exception exception) { reportError(exception); }
        }
    }
}
