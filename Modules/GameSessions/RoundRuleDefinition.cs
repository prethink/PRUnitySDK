using UnityEngine;

namespace PRGameSessions
{
    public abstract class RoundRuleDefinition : ScriptableObject
    {
        public abstract RoundBehaviour CreateRuntime();
    }
}
