using System;

namespace PRGameSessions
{
    public sealed class TimeLimitRule : RoundBehaviour
    {
        private readonly float duration;
        private float elapsed;

        public TimeLimitRule(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            this.duration = duration;
        }

        public override void Tick(RoundContext context, float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= duration) context.TryEnd(new RoundResult(SessionEndReason.TimeLimit));
        }
    }
}
