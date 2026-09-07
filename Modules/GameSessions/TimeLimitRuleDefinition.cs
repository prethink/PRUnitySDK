using UnityEngine;

namespace PRGameSessions
{
    [CreateAssetMenu(menuName = "PRUnitySDK/Game Sessions/Time Limit Rule")]
    public sealed class TimeLimitRuleDefinition : RoundRuleDefinition
    {
        [SerializeField, Min(0.01f)] private float duration = 120f;
        public override RoundBehaviour CreateRuntime() => new TimeLimitRule(duration);
    }
}
