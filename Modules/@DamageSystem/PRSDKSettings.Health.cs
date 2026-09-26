using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Настройки здоровья проекта.
    /// </summary>
    [field: SerializeField]
    public HealthSettings Health { get; private set; } = new();
}
