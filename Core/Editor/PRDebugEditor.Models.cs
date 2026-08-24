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
}
