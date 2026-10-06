using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Настройки запуска игры.
    /// </summary>
    [field: SerializeField]
    public BootstrapSettings Bootstrap { get; private set; } = new();
}
