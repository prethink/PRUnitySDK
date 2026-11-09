using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Формулы брони и сопротивлений проекта.
    /// </summary>
    [field: SerializeField]
    public DefenseSettings Defense { get; private set; } = new();
}
