using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PRGameSessions.Editor
{
    /// <summary>
    /// Проверки контроллера без запуска карты, изменения сцены и очистки глобальной шины.
    /// </summary>
    public static class GameSessionsChecks
    {
        [MenuItem("PRUnitySDK/Game Sessions/Run Checks")]
        public static void Run()
        {
            var failures = new List<string>();
            var checks = new Action[]
            {
                DisabledByDefault, RoundOrder, StaleRoundCannotEndNext, ScopeCleanupIsIsolated,
                CleanupContinuesAfterFailure, TimerEndsRound, ModeFailureClosesScope,
                ParticipantsSurviveRounds, EndRequestedInsideCallback, BusSnapshotIsStable,
                DisableEndsSession, NestedTransitionIsRejected, DeferredSessionEnd
            };
            foreach (var check in checks)
            {
                try { check(); }
                catch (Exception exception) { failures.Add(check.Method.Name + ": " + exception); }
            }
            var report = new Report { total = checks.Length, passed = checks.Length - failures.Count,
                failures = failures.ToArray(), utc = DateTime.UtcNow.ToString("O") };
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/GameSessionsChecks.json", JsonUtility.ToJson(report, true));
            if (failures.Count > 0) throw new Exception(string.Join("\n", failures));
            Debug.Log($"GameSessions checks: {report.passed}/{report.total} passed.");
        }

        private static GameSessionsController Create(Action<SessionEvent> notify = null)
        {
            var service = new GameSessionsController(notify ?? (_ => { }), _ => { });
            Require(service.SetEnabled(true));
            Require(service.TryStartSession("test-map"));
            return service;
        }

        private static RoundPlan Plan() => new RoundPlan("manual", () => new ManualRoundMode());
        private static void Require(bool value) { if (!value) throw new Exception("Assertion failed."); }

        private static void DisabledByDefault()
        {
            var service = new GameSessionsController(_ => { }, _ => { });
            Require(service.ReadySignal.IsReady && !service.Enabled && !service.TryStartSession("map"));
        }

        private static void RoundOrder()
        {
            var events = new List<SessionEventKind>();
            var service = Create(e => events.Add(e.Kind));
            Require(service.TryPrepareRound(Plan()));
            var round = service.CurrentRound;
            Require(!service.TryPrepareRound(Plan()));
            Require(service.TryStartRound(round.RoundId));
            Require(!service.TryStartRound(round.RoundId));
            Require(round.TryEnd(new RoundResult()));
            Require(!round.TryEnd(new RoundResult()));
            Require(string.Join(",", events) == "SessionStarted,RoundPreparing,RoundStarted,RoundEnding,RoundEnded");
        }

        private static void StaleRoundCannotEndNext()
        {
            var service = Create();
            service.TryPrepareRound(Plan());
            var old = service.CurrentRound;
            old.TryEnd(new RoundResult());
            service.TryPrepareRound(Plan());
            Require(!old.TryEnd(new RoundResult()));
            Require(old.RoundId != service.CurrentRound.RoundId && service.CurrentRound.Number == 2);
        }

        private static void ScopeCleanupIsIsolated()
        {
            var service = Create();
            var sessionResource = new Resource();
            service.SessionEntities.Own(sessionResource);
            service.TryPrepareRound(Plan());
            var round = service.CurrentRound;
            var roundResource = new Resource();
            round.Entities.Own(roundResource);
            round.TryEnd(new RoundResult());
            Require(roundResource.Releases == 1 && sessionResource.Releases == 0);
            round.Entities.Dispose();
            Require(roundResource.Releases == 1);
            service.TryEndSession(service.SessionId);
            Require(sessionResource.Releases == 1);
        }

        private static void CleanupContinuesAfterFailure()
        {
            var service = Create();
            service.TryPrepareRound(Plan());
            var resource = new Resource();
            service.CurrentRound.Entities.Own(new Resource { Throw = true });
            service.CurrentRound.Entities.Own(resource);
            service.CurrentRound.TryEnd(new RoundResult());
            Require(resource.Releases == 1 && service.Snapshot.Phase == RoundPhase.Ended);
        }

        private static void TimerEndsRound()
        {
            var service = Create();
            service.TryPrepareRound(new RoundPlan("timed", () => new ManualRoundMode(), () => new TimeLimitRule(2)));
            service.TryStartRound(service.CurrentRound.RoundId);
            service.Tick(1);
            Require(service.Snapshot.Phase == RoundPhase.Playing);
            service.Tick(1);
            Require(service.Snapshot.Result.Reason == SessionEndReason.TimeLimit);
        }

        private static void ModeFailureClosesScope()
        {
            var service = Create();
            var mode = new FailingMode();
            Require(!service.TryPrepareRound(new RoundPlan("failure", () => mode)));
            Require(mode.Resource.Releases == 1 && mode.Stopped);
            Require(service.Snapshot.Result.Reason == SessionEndReason.Error);
        }

        private static void ParticipantsSurviveRounds()
        {
            var service = Create();
            Require(service.TryJoin(new SessionParticipant(42, "red")));
            service.TryPrepareRound(Plan());
            service.CurrentRound.TryEnd(new RoundResult(winnerPlayerId: 42));
            Require(service.Snapshot.Participants.Count == 1 && service.Snapshot.Result.WinnerPlayerId == 42);
            Require(!service.TryJoin(new SessionParticipant(42)));
            Require(service.TryLeave(42) && service.Snapshot.Participants.Count == 0);
        }

        private static void EndRequestedInsideCallback()
        {
            GameSessionsController service = null;
            bool accepted = false;
            service = Create(e =>
            {
                if (e.Kind == SessionEventKind.RoundStarted)
                    accepted = service.CurrentRound.TryEnd(new RoundResult());
            });
            service.TryPrepareRound(Plan());
            service.TryStartRound(service.CurrentRound.RoundId);
            Require(accepted && service.Snapshot.Phase == RoundPhase.Ended);
        }

        private static void BusSnapshotIsStable()
        {
            var listener = new Listener();
            EventBus.Subscribe(listener);
            try
            {
                var service = new GameSessionsController(reportError: _ => { });
                service.SetEnabled(true);
                service.TryStartSession("map");
                service.TryPrepareRound(Plan());
                var snapshot = listener.Last.Snapshot;
                service.CurrentRound.TryEnd(new RoundResult());
                service.TryPrepareRound(Plan());
                Require(snapshot.RoundNumber == 1 && snapshot.Phase == RoundPhase.Preparing);
                Require(listener.Last.Snapshot.RoundNumber == 2);
            }
            finally { EventBus.Unsubscribe(listener); }
        }

        private static void DisableEndsSession()
        {
            var service = Create();
            service.TryPrepareRound(Plan());
            Require(service.SetEnabled(false));
            Require(service.Snapshot.State == SessionState.Ended && service.CurrentRound.Entities.IsClosed);
            Require(!service.TryPrepareRound(Plan()));
        }

        private static void NestedTransitionIsRejected()
        {
            GameSessionsController service = null;
            bool nested = true;
            service = Create(e =>
            {
                if (e.Kind == SessionEventKind.RoundEnded) nested = service.TryPrepareRound(Plan());
            });
            service.TryPrepareRound(Plan());
            service.CurrentRound.TryEnd(new RoundResult());
            Require(!nested && service.TryPrepareRound(Plan()));
        }

        private static void DeferredSessionEnd()
        {
            GameSessionsController service = null;
            service = Create(e =>
            {
                if (e.Kind == SessionEventKind.RoundPreparing)
                    service.TryEndSession(e.Snapshot.SessionId, SessionEndReason.Cancelled);
            });
            service.TryPrepareRound(Plan());
            Require(service.Snapshot.State == SessionState.Ended && service.CurrentRound.Entities.IsClosed);
        }

        private sealed class Listener : IGameSessionsEvents
        {
            public SessionEvent Last;
            public void OnSessionEvent(SessionEvent notification) => Last = notification;
        }

        private sealed class Resource : IDisposable
        {
            public int Releases;
            public bool Throw;
            public void Dispose() { Releases++; if (Throw) throw new Exception("Expected cleanup failure."); }
        }

        private sealed class FailingMode : RoundBehaviour
        {
            public readonly Resource Resource = new();
            public bool Stopped;
            public override void Prepare(RoundContext context)
            {
                context.Entities.Own(Resource);
                throw new Exception("Expected preparation failure.");
            }
            public override void Stop(RoundContext context, RoundResult result) => Stopped = true;
        }

        [Serializable]
        private sealed class Report
        {
            public int total;
            public int passed;
            public string[] failures;
            public string utc;
        }
    }
}
