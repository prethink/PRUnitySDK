using System;
using System.Linq;
using UnityEditor;

public partial class PRDebugEditor
{
    private void RefreshSnapshot()
    {
        nextRefresh = EditorApplication.timeSinceStartup + Math.Max(0.1d, refreshInterval);
        ClearSnapshot();

        if (!EditorApplication.isPlaying || !PRUnitySDK.IsInitialized)
        {
            lastRefreshUtc = DateTime.UtcNow;
            return;
        }

        try
        {
            CaptureInitialization();
            CapturePause();
            CapturePlayers();
            CaptureEntities();
            CapturePools();
            CaptureFlags();
        }
        catch (Exception exception)
        {
            snapshotError = $"Snapshot failed: {exception.GetType().Name}: {exception.Message}";
        }

        lastRefreshUtc = DateTime.UtcNow;
    }

    private void ClearSnapshot()
    {
        players.Clear();
        entities.Clear();
        pools.Clear();
        flagResolvers.Clear();
        initializationEntries.Clear();
        snapshotError = null;
        pause = default;
        humanCount = aiCount = 0;
        entityTotal = entityOnScene = entityInPool = 0;
        initializationTotalMilliseconds = 0d;
    }

    private void CaptureInitialization()
    {
        foreach (var entry in PRUnitySDK.InitializationHistory)
        {
            initializationEntries.Add(new InitializationRow
            {
                Category = entry.Category,
                Name = entry.Name,
                ContractType = entry.ContractType?.FullName ?? "<unknown>",
                ImplementationType = entry.ImplementationType?.FullName ?? "<unknown>",
                ContractTypeReference = entry.ContractType,
                ImplementationTypeReference = entry.ImplementationType,
                DurationMilliseconds = entry.DurationMilliseconds
            });
            initializationTotalMilliseconds += entry.DurationMilliseconds;
        }
    }

    private void CapturePause()
    {
        var manager = PRUnitySDK.PauseManager;
        pause = new PauseSnapshot(manager.IsProjectPaused, manager.IsLogicPaused, manager.IsFocusPaused,
            manager.IsMusicPaused, manager.IsTutorialPaused, manager.IsCutScenePaused);
    }

    private void CapturePlayers()
    {
        var tracker = PRUnitySDK.Trackers.Players;
        humanCount = tracker.HumanCount;
        aiCount = tracker.AICount;

        foreach (var player in tracker.Players)
        {
            if (player == null)
                continue;

            players.Add(new PlayerRow
            {
                GameObject = player.gameObject,
                PlayerId = player.PlayerId,
                EntityId = player.Id,
                Type = player.PlayerType.ToString(),
                Name = SafeValue(() => player.Info?.GetName(), "-"),
                Team = SafeValue(() => player.PlayerTeam?.Name, "-"),
                Ready = player is IReadySignalProvider ready ? ready.ReadySignal?.IsReady : null,
                Points = player.Points,
                Kills = player.Kills,
                Deaths = player.Deaths
            });
        }

        players.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));
    }

    private void CaptureEntities()
    {
        var tracker = PRUnitySDK.Trackers.Entities;
        entityTotal = tracker.GetExistsEntityCount();
        entityOnScene = tracker.GetEntityOnSceneCount();
        entityInPool = tracker.GetEntityInPoolCount();

        foreach (var item in tracker.RegisteredEntity)
        {
            long onScene = tracker.GetExactEntityOnSceneCount(item.Key);
            var entity = tracker.Entities.FirstOrDefault(value =>
                value != null && !value.IsNull() && value.EntityType == item.Key);

            entities.Add(new EntityRow
            {
                Icon = SafeValue(() => entity?.Info?.GetIcon(), null),
                Type = item.Key?.ToString() ?? "<null>",
                Registered = item.Value,
                OnScene = onScene,
                Hidden = Math.Max(0, item.Value - onScene),
                InPool = tracker.GetExactEntityInPoolCount(item.Key)
            });
        }

        entities.Sort((left, right) => right.Registered.CompareTo(left.Registered));
    }

    private static T SafeValue<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter != null ? getter() : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void CapturePools()
    {
        var manager = PRUnitySDK.Managers.ObjectPool;
        if (manager != null)
            pools.AddRange(manager.GenerateReport().OrderByDescending(item => item.TotalCount)
                .ThenBy(item => item.Type).ThenBy(item => item.Category));
    }

    private void CaptureFlags()
    {
        var manager = PRUnitySDK.Managers.Flags;
        if (manager != null)
        {
            flagResolvers.Add(new FlagResolverRow("Global / Project", null, manager.Global.GetDebugSnapshot()));
            int index = 0;
            foreach (var resolver in manager.Scenes)
                flagResolvers.Add(new FlagResolverRow($"Global / Scene {index++}", null, resolver.GetDebugSnapshot()));
        }

        foreach (var component in FindObjectsOfType<FlagResolverMono>())
            flagResolvers.Add(new FlagResolverRow(component.name, component.gameObject, component.Link.GetDebugSnapshot()));
    }
}
