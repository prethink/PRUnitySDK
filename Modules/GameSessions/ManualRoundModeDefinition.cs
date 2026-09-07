using UnityEngine;

namespace PRGameSessions
{
    [CreateAssetMenu(menuName = "PRUnitySDK/Game Sessions/Manual Mode")]
    public sealed class ManualRoundModeDefinition : RoundModeDefinition
    {
        public override RoundBehaviour CreateRuntime() => new ManualRoundMode();
    }
}
