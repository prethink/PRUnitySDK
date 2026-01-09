using System;
using UnityEngine;

public partial class PRGameSettings
{
    [field: SerializeField] public BuildSettings BuildSettings { get; private set; }
}


[Serializable]
public class BuildSettings
{
    [field: SerializeField, Range(0,10)] public int DebugLogLevel { get; private set; }
}