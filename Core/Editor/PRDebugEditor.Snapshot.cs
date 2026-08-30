using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    private void RefreshSnapshot()
    {
        nextRefresh = EditorApplication.timeSinceStartup + Math.Max(0.1d, refreshInterval);
        ClearSnapshot();

        if (!EditorApplication.isPlaying)
        {
            lastRefreshUtc = DateTime.UtcNow;
            return;
        }

        try
        {
            CaptureInitialization();
            CaptureEventHistory();

            if (!PRUnitySDK.IsInitialized)
            {
                CaptureProblems();
                lastRefreshUtc = DateTime.UtcNow;
                return;
            }

            CapturePause();
            CaptureTimeScale();
            CaptureSaveDiagnostics();
            CapturePlayers();
            CaptureEntities();
            CapturePools();
            CaptureFlags();
            CaptureMonoWindows();
            CaptureBackgroundTasks();
            CaptureGameRules();
            CaptureProblems();
        }
        catch (Exception exception)
        {
            snapshotError = $"Snapshot failed: {exception.GetType().Name}: {exception.Message}";
        }

        lastRefreshUtc = DateTime.UtcNow;
    }

    private void ClearSnapshot()
    {
        problems.Clear();
        players.Clear();
        entities.Clear();
        entityInstances.Clear();
        pools.Clear();
        flagResolvers.Clear();
        flagProviders.Clear();
        initializationEntries.Clear();
        monoWindows.Clear();
        backgroundTasks.Clear();
        statRules.Clear();
        timeScaleRows.Clear();
        eventRows.Clear();
        aggregatedEventRows.Clear();
        snapshotError = null;
        pause = default;
        humanCount = aiCount = 0;
        entityTotal = entityOnScene = entityInPool = 0;
        saveState = GameSaveState.NotStarted;
        saveCreationTimeUtc = null;
        lastSaveTimeUtc = null;
        hasLoadedSave = false;
        canStartSave = false;
        saveCooldownRemainingSeconds = 0;
        timeScaleCombineMode = "-";
        timeScaleSubscriberCount = 0;
        hasActiveTemporaryTimeScales = false;
        globalTemporaryTimeScaleActive = false;
        initializationTotalMilliseconds = 0d;
    }

    private void DiscoverHealthChecks()
    {
        healthChecks.Clear();
        healthCheckLoadErrors.Clear();

        foreach (Type checkType in TypeCache.GetTypesDerivedFrom<IPRDebugHealthCheck>())
        {
            if (checkType.IsAbstract || checkType.ContainsGenericParameters)
                continue;

            try
            {
                if (Activator.CreateInstance(checkType) is IPRDebugHealthCheck check)
                    healthChecks.Add(check);
            }
            catch (Exception exception)
            {
                healthCheckLoadErrors.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Health Check",
                    "CheckCreationFailed",
                    $"Cannot create health check '{checkType.FullName}': {exception.GetBaseException().Message}",
                    sourceType: checkType));
            }
        }

        healthChecks.Sort((left, right) => string.CompareOrdinal(
            left.GetType().FullName, right.GetType().FullName));
    }

    private void CaptureProblems()
    {
        problems.AddRange(healthCheckLoadErrors);

        foreach (IPRDebugHealthCheck check in healthChecks)
        {
            try
            {
                var results = check.Check();
                if (results == null)
                    continue;

                foreach (PRDebugProblem problem in results)
                {
                    if (problem != null)
                        problems.Add(problem);
                }
            }
            catch (Exception exception)
            {
                Type checkType = check.GetType();
                problems.Add(new PRDebugProblem(PRDebugProblemSeverity.Error, "Health Check",
                    "CheckExecutionFailed",
                    $"Health check '{checkType.FullName}' failed: {exception.GetBaseException().Message}",
                    sourceType: checkType));
            }
        }

        problems.Sort((left, right) =>
        {
            int severity = right.Severity.CompareTo(left.Severity);
            if (severity != 0)
                return severity;

            int category = string.CompareOrdinal(left.Category, right.Category);
            return category != 0 ? category : string.CompareOrdinal(left.Code, right.Code);
        });
    }

    private void CaptureMonoWindows()
    {
        MonoWindowsTracker tracker = PRUnitySDK.Trackers.MonoWindows;
        foreach (MonoWindowBase window in tracker.Elements)
        {
            if (window == null)
                continue;

            monoWindows.Add(new MonoWindowRow
            {
                Window = window,
                GameObject = window.gameObject,
                Type = window.GetType(),
                Key = SafeValue(() => window.Key?.Value, "<null>"),
                Visible = SafeValue(() => window.IsVisible, false),
                Active = window.gameObject.activeInHierarchy,
                Current = tracker.CurrentWindow == window
            });
        }

        monoWindows.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
    }

    private void CaptureBackgroundTasks()
    {
        BackgroundTaskTracker tracker = PRUnitySDK.Trackers.BackgroundTasks;

        foreach (IBackgroundTask task in tracker.Elements)
        {
            if (task == null || task.IsNull())
                continue;

            BackgroundTaskRuntime runtime = task.Runtime;

            // Задача может считать время по игровой шкале, поэтому «сейчас» берётся
            // из той же шкалы, что использует трекер при планировании.
            float now = SafeValue(() => runtime.CurrentTime, 0f);
            float lastRun = SafeValue(() => runtime.LastRunRealTime, -1f);

            backgroundTasks.Add(new BackgroundTaskRow
            {
                Task = task,
                Component = task as Component,
                Type = task.GetType(),
                Key = SafeValue(() => task.Key?.Value, "<null>"),
                Name = SafeValue(() => task.Name, "<null>"),
                Status = SafeValue(() => runtime.Status, BackgroundTaskStatus.Pending),
                RepeatSeconds = SafeValue(() => task.RepeatSeconds, 0f),
                UseGameTime = SafeValue(() => task.UseGameTime, false),
                SecondsToNextRun = SafeValue(() => runtime.NextRunTime - now, 0f),
                SecondsSinceLastRun = lastRun < 0f
                    ? -1f
                    : SafeValue(() => PRTime.Instance.RealTime - lastRun, -1f),
                ExecutedCount = SafeValue(() => runtime.ExecutedCount, 0),
                SkippedCount = SafeValue(() => runtime.SkippedCount, 0),
                ErrorCount = SafeValue(() => runtime.ErrorCount, 0),
                ConsecutiveErrors = SafeValue(() => runtime.ConsecutiveErrors, 0),
                LastRunDurationMs = SafeValue(() => runtime.LastRunDurationMs, 0d),
                LastError = SafeValue(() => runtime.LastError == null
                    ? null
                    : $"{runtime.LastError.GetType().Name}: {runtime.LastError.Message}", null),
                WatchedValue = ReadWatchedValue(task)
            });
        }

        backgroundTasks.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
    }

    private void CaptureGameRules()
    {
        foreach (Enumeration stat in GameRules.Stats.ToArray())
        {
            if (stat == null)
                continue;

            IReadOnlyList<StatRuleBase> rules = SafeValue(() => GameRules.GetRules(stat), null);
            if (rules == null)
                continue;

            for (int index = 0; index < rules.Count; index++)
            {
                StatRuleBase rule = rules[index];
                if (rule == null)
                    continue;

                statRules.Add(new StatRuleRow
                {
                    Stat = stat,
                    StatName = SafeValue(() => stat.Value, "<null>"),
                    RuleType = rule.GetType(),
                    Priority = SafeValue(() => rule.Priority, 0),
                    Order = index,
                    Parameters = DescribeRule(rule)
                });
            }
        }

        statRules.Sort((left, right) =>
        {
            int stat = string.CompareOrdinal(left.StatName, right.StatName);
            return stat != 0 ? stat : left.Order.CompareTo(right.Order);
        });
    }

    /// <summary>
    /// Собирает собственные параметры правила: свойства, объявленные в самом типе,
    /// без унаследованных от <see cref="StatRuleBase"/>.
    /// </summary>
    /// <remarks>
    /// Так новые типы правил попадают в окно без правок редактора: у `MinValueRule`
    /// покажется `MinValue`, у своего правила - его собственные поля.
    /// </remarks>
    private static string DescribeRule(StatRuleBase rule)
    {
        return SafeValue(() =>
        {
            PropertyInfo[] properties = rule.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (properties.Length == 0)
                return "-";

            var parts = new List<string>(properties.Length);
            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                object value = property.GetValue(rule);
                parts.Add($"{property.Name}={value}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }, "<error>");
    }

    /// <summary>
    /// Читает текущее значение задачи-наблюдателя.
    /// Тип значения известен только в рантайме, поэтому состояние берётся рефлексией.
    /// </summary>
    /// <remarks>
    /// Поиск идёт по интерфейсу <c>IWatcherTask&lt;&gt;</c>, а не по базовому классу:
    /// наблюдателем может быть и обычная задача, и компонент сцены, а общий у них
    /// только контракт.
    /// </remarks>
    private static string ReadWatchedValue(IBackgroundTask task)
    {
        Type watcherInterface = null;
        foreach (Type contract in task.GetType().GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IWatcherTask<>))
            {
                watcherInterface = contract;
                break;
            }
        }

        if (watcherInterface == null)
            return null;

        return SafeValue(() =>
        {
            object state = watcherInterface.GetProperty("Watcher")?.GetValue(task);
            if (state == null)
                return "<not read>";

            Type stateType = state.GetType();

            object hasValue = stateType.GetProperty("HasValue")?.GetValue(state);
            if (hasValue is not true)
                return "<not read>";

            object value = stateType.GetProperty("CurrentValue")?.GetValue(state);
            return value?.ToString() ?? "null";
        }, "<error>");
    }

    private void OnEventBusRaised(Type eventType, int subscriberCount)
    {
        if (!captureEvents)
            return;

        DateTime timestampUtc = DateTime.UtcNow;
        lock (eventHistoryLock)
        {
            if (eventType != null && AggregatedEventTypes.Contains(eventType))
            {
                if (!aggregatedEventHistory.TryGetValue(eventType, out EventBusAccumulator accumulator))
                {
                    accumulator = new EventBusAccumulator
                    {
                        FirstTimestampUtc = timestampUtc
                    };
                    aggregatedEventHistory.Add(eventType, accumulator);
                }

                accumulator.Count++;
                accumulator.LastTimestampUtc = timestampUtc;
                accumulator.SubscriberCount = subscriberCount;
            }
            else
            {
                long sequence = Interlocked.Increment(ref eventSequence);
                while (eventHistory.Count >= EventHistoryCapacity)
                    eventHistory.Dequeue();

                eventHistory.Enqueue(new EventBusRow(sequence, timestampUtc, eventType, subscriberCount));
            }
        }

        eventHistoryDirty = true;
    }

    private void CaptureEventHistory()
    {
        lock (eventHistoryLock)
        {
            eventRows.AddRange(eventHistory);

            foreach (var item in aggregatedEventHistory.OrderBy(item => item.Key.FullName, StringComparer.Ordinal))
            {
                EventBusAccumulator accumulator = item.Value;
                aggregatedEventRows.Add(new AggregatedEventBusRow(item.Key, accumulator.Count,
                    accumulator.FirstTimestampUtc, accumulator.LastTimestampUtc,
                    accumulator.SubscriberCount));
            }
        }
    }

    private void ClearEventHistory()
    {
        lock (eventHistoryLock)
        {
            eventHistory.Clear();
            aggregatedEventHistory.Clear();
        }

        eventRows.Clear();
        aggregatedEventRows.Clear();
        eventHistoryDirty = false;
        Repaint();
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

    private void CaptureTimeScale()
    {
        PRTimeScale timeScale = PRTimeScale.Instance;
        timeScaleCombineMode = PRUnitySDK.Settings.Project.TimeScaleCombineMode.ToString();
        timeScaleSubscriberCount = EventBus.GetSubscriberCount<IOnPRTimeScaleChange>();
        hasActiveTemporaryTimeScales = timeScale.HasActiveTemporaryTimeScales;
        globalTemporaryTimeScaleActive = timeScale.IsTimeScaleTemporaryActive(
            PRTimeScaleEnumerationProvider.Global);

        foreach (Enumeration layer in new PRTimeScaleEnumerationProvider().GetOptions()
                     .Where(value => value != null)
                     .GroupBy(value => value.Value, StringComparer.Ordinal)
                     .Select(group => group.First())
                     .OrderBy(value => value == PRTimeScaleEnumerationProvider.Global ? 0 : 1)
                     .ThenBy(value => value.Value, StringComparer.Ordinal))
        {
            float value = timeScale.GetTimeScale(layer);
            float resolvedValue = layer == PRTimeScaleEnumerationProvider.Global
                ? timeScale.Resolve()
                : timeScale.Resolve(layer);

            timeScaleRows.Add(new TimeScaleRow(layer, value, resolvedValue));
        }
    }

    private void CaptureSaveDiagnostics()
    {
        GameManager manager = PRUnitySDK.Managers?.Game;
        if (manager == null)
            return;

        saveState = manager.SaveState;
        saveCreationTimeUtc = manager.SaveCreationTimeUtc;
        lastSaveTimeUtc = manager.LastSaveTimeUtc;
        hasLoadedSave = manager.HasLoadedSave;
        canStartSave = manager.CanStartSave();
        saveCooldownRemainingSeconds = manager.SaveCooldownRemainingSeconds;
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
                Name = SafeValue(() => player.Description?.GetName(), "-"),
                Team = SafeValue(() => player.PlayerTeam?.Name, "-"),
                Ready = player is IReadySignalProvider ready ? ready.ReadySignal?.IsReady : null,
                Points = player.Points,
                Kills = player.Kills,
                Deaths = player.Deaths
            });
        }

        players.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));
    }

    /// <summary>
    /// Разбивает тип сущности на виды предметов.
    /// </summary>
    /// <remarks>
    /// Сводной строки мало: все шапки приходят как один тип <c>Hat</c>, и по ней не видно,
    /// какие именно предметы на сцене. Вид определяется по <c>Info</c> - то есть по
    /// определению, из которого сущность создана.
    /// <para>
    /// Считаются только живые экземпляры: сколько предметов каждого вида зарегистрировано,
    /// трекер не знает - он ведёт счёт по типу.
    /// </para>
    /// </remarks>
    private static void CaptureEntityKinds(EntityRow row, IReadOnlyList<IEntity> ofType)
    {
        var kinds = new Dictionary<string, EntityKindRow>(StringComparer.Ordinal);

        foreach (var entity in ofType)
        {
            string name = SafeValue(() => entity.Description?.GetName(), null);
            if (string.IsNullOrWhiteSpace(name))
                name = "<no info>";

            if (!kinds.TryGetValue(name, out EntityKindRow kind))
            {
                kind = new EntityKindRow
                {
                    Name = name,
                    Icon = SafeValue(() => entity.Description?.GetIcon(), null),
                    Quality = SafeValue(() => entity.Description?.GetQuality().ToString(), "-")
                };
                kinds.Add(name, kind);
            }

            kind.Total++;
            if (entity.OnScene) kind.OnScene++;
            if (entity.InPool) kind.InPool++;
        }

        row.Kinds.AddRange(kinds.Values);
        row.Kinds.Sort((left, right) =>
        {
            int byCount = right.Total.CompareTo(left.Total);
            return byCount != 0 ? byCount : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void CaptureEntities()
    {
        var tracker = PRUnitySDK.Trackers.Entities;
        var trackedEntities = tracker.Entities;
        entityTotal = tracker.GetExistsEntityCount();
        entityOnScene = tracker.GetEntityOnSceneCount();
        entityInPool = tracker.GetEntityInPoolCount();

        foreach (var item in tracker.RegisteredEntity)
        {
            long onScene = tracker.GetExactEntityOnSceneCount(item.Key);
            var ofType = trackedEntities
                .Where(value => value != null && !value.IsNull() && value.EntityType == item.Key)
                .ToArray();

            var row = new EntityRow
            {
                Icon = SafeValue(() => ofType.FirstOrDefault()?.Description?.GetIcon(), null),
                Type = item.Key?.ToString() ?? "<null>",
                Registered = item.Value,
                OnScene = onScene,
                Hidden = Math.Max(0, item.Value - onScene),
                InPool = tracker.GetExactEntityInPoolCount(item.Key)
            };

            CaptureEntityKinds(row, ofType);
            entities.Add(row);
        }

        entities.Sort((left, right) => right.Registered.CompareTo(left.Registered));

        foreach (var entity in trackedEntities)
        {
            if (entity == null || entity.IsNull())
                continue;

            string poolStatus = "-";
            if (entity is IPoolable poolable)
            {
                poolStatus = entity.InPool
                    ? "In pool"
                    : poolable.PoolBehaviour?.IsInitialize == true ? "Ready" : "No";
            }

            entityInstances.Add(new EntityInstanceRow
            {
                Entity = entity,
                GameObject = entity.gameObject,
                Id = entity.Id,
                Type = entity.EntityType?.ToString() ?? "<null>",
                Name = SafeValue(() => entity.Description?.GetName(), "-"),
                LifeTime = entity.LifeTime.ToString(),
                PoolStatus = poolStatus,
                OnScene = entity.OnScene,
                InPool = entity.InPool
            });
        }

        entityInstances.Sort((left, right) =>
        {
            int typeComparison = string.Compare(left.Type, right.Type, StringComparison.OrdinalIgnoreCase);
            return typeComparison != 0 ? typeComparison : left.Id.CompareTo(right.Id);
        });
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
        CaptureFlagProviders();

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

    private void CaptureFlagProviders()
    {
        foreach (Type providerType in TypeCache.GetTypesDerivedFrom<FlagsProviderBase>())
        {
            if (providerType.IsAbstract || providerType.ContainsGenericParameters)
                continue;

            try
            {
                if (Activator.CreateInstance(providerType) is not FlagsProviderBase provider)
                    continue;

                var flags = provider.GetOptions()
                    .Where(flag => flag != null)
                    .GroupBy(flag => flag.Value, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(flag => flag.Value, StringComparer.Ordinal)
                    .ToArray();
                if (flags.Length > 0)
                    flagProviders.Add(new FlagProviderRow(providerType, flags));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Cannot read flags from provider '{providerType.FullName}': {exception.Message}");
            }
        }

        flagProviders.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        selectedFlagProviderIndex = Mathf.Clamp(selectedFlagProviderIndex, 0, Math.Max(0, flagProviders.Count - 1));

        int flagCount = flagProviders.Count > 0 ? flagProviders[selectedFlagProviderIndex].Flags.Count : 0;
        selectedFlagIndex = Mathf.Clamp(selectedFlagIndex, 0, Math.Max(0, flagCount - 1));
    }
}
