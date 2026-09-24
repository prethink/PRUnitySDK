using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Правила урона проекта.
    /// </summary>
    [field: SerializeField]
    public DamageRulesSettings DamageRules { get; private set; } = new();
}
