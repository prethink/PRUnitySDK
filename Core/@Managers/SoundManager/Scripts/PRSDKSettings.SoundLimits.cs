using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Пределы позиционных звуков.
    /// </summary>
    [field: SerializeField]
    public SoundLimitSettings SoundLimits { get; private set; } = new();
}
