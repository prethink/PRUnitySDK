using System;
using UnityEngine;

[Serializable]
public partial class PRProjectSettings
{
    [field: SerializeField] public ReleaseType ReleaseType { get; protected set; }
}

[Serializable]
public enum ReleaseType
{
    Debug,
    Release
}