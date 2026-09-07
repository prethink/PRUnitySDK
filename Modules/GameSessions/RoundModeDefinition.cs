using UnityEngine;

namespace PRGameSessions
{
    /// <summary>
    /// Ассет хранит конфигурацию; изменяемое состояние возвращает CreateRuntime.
    /// </summary>
    public abstract class RoundModeDefinition : ScriptableObject
    {
        [SerializeField] private string modeKey = "Manual";
        public string ModeKey => modeKey;
        public abstract RoundBehaviour CreateRuntime();
    }
}
