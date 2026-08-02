using UnityEngine;

public abstract class ReactionMonobehaviourBase<TStateManager, TContext> : PRMonoBehaviour, IStateReaction<TStateManager> 
    where TStateManager : IStateManager
{
    public TStateManager stateManager { get; protected set; }

    protected bool isInitialize;

    protected ReactionContext<TContext> reactionContext = new();

    public virtual bool CanReact()
    {
        return isInitialize;
    }

    public bool Initialize(TStateManager stateManager)
    {
        this.stateManager = stateManager;
        isInitialize = true;
        return isInitialize;
    }

    protected abstract void InternalReact();
}

public abstract class BackgroundReactionMonobehaviourBase<TStateManager> : ReactionMonobehaviourBase<TStateManager, NoContext>, IStateBackgroundReaction
        where TStateManager : IStateManager
{
    public bool TryReact() => reactionContext.TryReact(NoContext.Instance, CanReact, InternalReact);
}

public abstract class TriggerReactionMonobehaviourBase<TStateManager> : ReactionMonobehaviourBase<TStateManager, Collider>, IStateReactionTrigger
        where TStateManager : IStateManager
{
    public bool TryReact(Collider collider) => reactionContext.TryReact(collider, CanReact, InternalReact);
}

public abstract class EventReactionMonobehaviourBase<TStateManager> : ReactionMonobehaviourBase<TStateManager, EnumerationReactionArgs>, IStateReactionEvent
        where TStateManager : IStateManager
{
    public Enumeration EventKey { get; }

    public override bool CanReact()
    {
        return base.CanReact() && reactionContext.Context.Enumeration == EventKey;
    }

    public bool TryReact(EnumerationReactionArgs enumeration) => reactionContext.TryReact(enumeration, CanReact, InternalReact);
}


