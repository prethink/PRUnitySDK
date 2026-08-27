using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PRDebugEditor
{
    private sealed class InitializationRow
    {
        public PRInitializationCategory Category;
        public string Name;
        public string ContractType;
        public string ImplementationType;
        public Type ContractTypeReference;
        public Type ImplementationTypeReference;
        public double DurationMilliseconds;
    }

    private readonly struct PauseSnapshot
    {
        public readonly bool Project;
        public readonly bool Logic;
        public readonly bool Focus;
        public readonly bool Music;
        public readonly bool Tutorial;
        public readonly bool Cutscene;

        public PauseSnapshot(bool project, bool logic, bool focus, bool music, bool tutorial, bool cutscene)
        {
            Project = project;
            Logic = logic;
            Focus = focus;
            Music = music;
            Tutorial = tutorial;
            Cutscene = cutscene;
        }
    }

    private readonly struct TimeScaleRow
    {
        public readonly Enumeration Layer;
        public readonly float Value;
        public readonly float ResolvedValue;

        public TimeScaleRow(Enumeration layer, float value, float resolvedValue)
        {
            Layer = layer;
            Value = value;
            ResolvedValue = resolvedValue;
        }
    }

    private sealed class PlayerRow
    {
        public GameObject GameObject;
        public long PlayerId;
        public long EntityId;
        public long Points;
        public string Type;
        public string Name;
        public string Team;
        public bool? Ready;
        public int Kills;
        public int Deaths;
    }

    private sealed class EntityRow
    {
        /// <summary>
        /// Иконка первой доступной сущности этого типа.
        /// </summary>
        public Sprite Icon;

        public string Type;
        public long Registered;
        public long OnScene;
        public long Hidden;
        public long InPool;
    }

    private sealed class EntityInstanceRow
    {
        public IEntity Entity;
        public GameObject GameObject;
        public long Id;
        public string Type;
        public string Name;
        public string LifeTime;
        public string PoolStatus;
        public bool OnScene;
        public bool InPool;
    }

    private sealed class FlagResolverRow
    {
        public string Name { get; }
        public GameObject Owner { get; }
        public IReadOnlyList<FlagDebugInfo> Flags { get; }

        public FlagResolverRow(string name, GameObject owner, IReadOnlyList<FlagDebugInfo> flags)
        {
            Name = name;
            Owner = owner;
            Flags = flags;
        }
    }

    private sealed class FlagProviderRow
    {
        public Type Type { get; }
        public string Name { get; }
        public IReadOnlyList<Enumeration> Flags { get; }

        public FlagProviderRow(Type type, IReadOnlyList<Enumeration> flags)
        {
            Type = type;
            Name = type?.FullName ?? "<unknown>";
            Flags = flags;
        }
    }

    private sealed class MonoWindowRow
    {
        public MonoWindowBase Window;
        public GameObject GameObject;
        public Type Type;
        public string Key;
        public bool Visible;
        public bool Active;
        public bool Current;
    }

    private sealed class StatRuleRow
    {
        public Enumeration Stat;
        public string StatName;
        public Type RuleType;
        public int Priority;

        /// <summary>
        /// Порядковый номер правила внутри характеристики - порядок применения.
        /// </summary>
        public int Order;

        /// <summary>
        /// Собственные параметры правила, например <c>MinValue=0.2</c>.
        /// </summary>
        public string Parameters;
    }

    private sealed class BackgroundTaskRow
    {
        public IBackgroundTask Task;

        /// <summary>
        /// Компонент сцены, если задача живёт на объекте, иначе <c>null</c>.
        /// </summary>
        public Component Component;

        public Type Type;
        public string Key;
        public string Name;
        public BackgroundTaskStatus Status;
        public float RepeatSeconds;
        public bool UseGameTime;

        /// <summary>
        /// Сколько секунд осталось до следующего запуска.
        /// Отрицательное значение означает, что задача уже просрочена.
        /// </summary>
        public float SecondsToNextRun;

        /// <summary>
        /// Сколько секунд прошло с последнего запуска либо -1, если запусков не было.
        /// </summary>
        public float SecondsSinceLastRun;

        public int ExecutedCount;
        public int SkippedCount;
        public int ErrorCount;
        public int ConsecutiveErrors;
        public double LastRunDurationMs;
        public string LastError;

        /// <summary>
        /// Текущее значение для задач-наблюдателей либо <c>null</c> для обычных задач.
        /// </summary>
        public string WatchedValue;
    }

    private readonly struct EventBusRow
    {
        public readonly long Sequence;
        public readonly DateTime TimestampUtc;
        public readonly Type EventType;
        public readonly int SubscriberCount;

        public EventBusRow(long sequence, DateTime timestampUtc, Type eventType, int subscriberCount)
        {
            Sequence = sequence;
            TimestampUtc = timestampUtc;
            EventType = eventType;
            SubscriberCount = subscriberCount;
        }
    }

    private sealed class EventBusAccumulator
    {
        public long Count;
        public DateTime FirstTimestampUtc;
        public DateTime LastTimestampUtc;
        public int SubscriberCount;
    }

    private readonly struct AggregatedEventBusRow
    {
        public readonly Type EventType;
        public readonly long Count;
        public readonly DateTime FirstTimestampUtc;
        public readonly DateTime LastTimestampUtc;
        public readonly int SubscriberCount;

        public AggregatedEventBusRow(Type eventType, long count, DateTime firstTimestampUtc,
            DateTime lastTimestampUtc, int subscriberCount)
        {
            EventType = eventType;
            Count = count;
            FirstTimestampUtc = firstTimestampUtc;
            LastTimestampUtc = lastTimestampUtc;
            SubscriberCount = subscriberCount;
        }

        public double AverageCallsPerSecond
        {
            get
            {
                double seconds = (LastTimestampUtc - FirstTimestampUtc).TotalSeconds;
                return seconds > 0d ? Math.Max(0d, Count - 1L) / seconds : 0d;
            }
        }
    }
}
