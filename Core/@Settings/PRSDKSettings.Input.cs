using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Настройки ввода.
    /// </summary>
    [field: SerializeField]
    public InputSettings Input { get; private set; } = new();
}
