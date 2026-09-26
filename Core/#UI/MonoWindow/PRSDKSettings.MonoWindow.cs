using UnityEngine;

public partial class PRSDKSettings
{
    /// <summary>
    /// Переход окон по умолчанию.
    /// </summary>
    [field: SerializeField]
    public MonoWindowTransitionSettings WindowTransition { get; private set; } = new();
}
