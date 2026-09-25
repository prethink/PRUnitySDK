using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Настройки сущностей.
    /// </summary>
    [field: SerializeField]
    public EntitySettings Entity { get; private set; } = new();
}
