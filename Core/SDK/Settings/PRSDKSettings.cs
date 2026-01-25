using UnityEngine;

public partial class PRSDKSettings : ScriptableObjectSingleton<PRSDKSettings>
{
    [field: SerializeField] public PRProjectSettings Project { get; protected set; }
    [field: SerializeField] public DefaultSettings Default { get; protected set; }
    [field: SerializeField] public BotSettings Bot { get; protected set; }
    [field: SerializeField] public SceneTransitionSettings SceneTransition { get; protected set; }
    [field: SerializeField, Tooltip("Настройки качества")] public PRQualitySettings Quality { get; protected set; }
}