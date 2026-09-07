using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PRGameSessions
{
    /// <summary>
    /// Наличие компонента включает модуль для этой карты. Только владелец тикает и завершает сессию.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionSceneHost : PRMonoBehaviour
    {
        [SerializeField] private RoundModeDefinition firstMode;
        [SerializeField] private RoundRuleDefinition[] firstRules = Array.Empty<RoundRuleDefinition>();
        private IGameSessionsService service;
        private Guid ownedSessionId;
        private int sceneHandle;

        protected override void OnEnable()
        {
            base.OnEnable();
            sceneHandle = gameObject.scene.handle;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            PRUnitySDK.ReadySignal.SubscribeOnReady(Begin);
        }

        private void Begin()
        {
            if (!isActiveAndEnabled || ownedSessionId != Guid.Empty) return;
            service = PRUnitySDK.GameSessions;
            if (service == null || !service.SetEnabled(true)) return;
            var scene = gameObject.scene;
            var key = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            if (!service.TryStartSession(key))
            {
                PRLog.WriteWarning(this, "Another scene already owns the active game session.");
                return;
            }
            ownedSessionId = service.SessionId;
            try
            {
                if (firstMode != null) StartNextRound(firstMode, firstRules);
            }
            catch (Exception exception)
            {
                service.TryEndSession(ownedSessionId, SessionEndReason.Error);
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Вызывается после завершения предыдущего раунда; можно передать другой режим и набор правил.
        /// </summary>
        public bool StartNextRound(RoundModeDefinition mode, params RoundRuleDefinition[] rules)
        {
            if (service == null || ownedSessionId == Guid.Empty || service.SessionId != ownedSessionId) return false;
            if (mode == null) throw new ArgumentNullException(nameof(mode));
            var factories = new Func<RoundBehaviour>[rules?.Length ?? 0];
            for (int i = 0; i < factories.Length; i++)
            {
                var rule = rules[i];
                if (rule == null) throw new ArgumentException("Round rule is missing.", nameof(rules));
                factories[i] = rule.CreateRuntime;
            }
            return service.TryPrepareRound(new RoundPlan(mode.ModeKey, mode.CreateRuntime, factories)) &&
                service.TryStartRound(service.CurrentRound.RoundId);
        }

        protected override void PRUpdate()
        {
            if (service != null && ownedSessionId != Guid.Empty && service.SessionId == ownedSessionId)
                service.Tick(Time.deltaTime);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.handle == sceneHandle) EndOwned(SessionEndReason.SceneUnloaded);
        }

        private void EndOwned(SessionEndReason reason)
        {
            if (ownedSessionId == Guid.Empty) return;
            service?.TryEndSession(ownedSessionId, reason);
            ownedSessionId = Guid.Empty;
        }

        protected override void OnDisable()
        {
            PRUnitySDK.ReadySignal.UnSubscribe(Begin);
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            EndOwned(SessionEndReason.Cancelled);
            base.OnDisable();
        }
    }
}
