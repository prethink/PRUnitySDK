using System;
using UnityEngine;

[Serializable]
public partial class PRProjectSettings
{
    [field: SerializeField] public ReleaseType ReleaseType { get; protected set; }
    [field: SerializeField, Range(0, 10)] public int DebugLogLevel { get; private set; }

    [field: SerializeField] public ResolveStrategy ResolveStrategy { get; protected set; }
}

[Serializable]
public enum ReleaseType
{
    Debug,
    Release
}

[Serializable]
public enum ResolveStrategy
{
    PriorityResolver,
    FirstResolve
}