namespace PRGameSessions
{
    /// <summary>
    /// Отдельный runtime-экземпляр режима или правила создаётся для каждого раунда.
    /// </summary>
    public abstract class RoundBehaviour
    {
        public virtual void Prepare(RoundContext context) { }
        public virtual void Start(RoundContext context) { }
        public virtual void Tick(RoundContext context, float deltaTime) { }
        public virtual void Stop(RoundContext context, RoundResult result) { }
    }
}
