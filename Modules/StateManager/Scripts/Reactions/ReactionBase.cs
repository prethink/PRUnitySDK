using System;
using UnityEngine;

public abstract class ReactionBase<TStateManager, TContext> : IStateReaction<TStateManager>
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
        if(isInitialize)
            return false;

        this.stateManager = stateManager;
        isInitialize = true;
        return isInitialize;
    }

    protected abstract void InternalReact();

    public ReactionBase(TStateManager stateManager)
    {
        Initialize(stateManager);
    }
}

public abstract class BackgroundReactionBase<TStateManager> : ReactionBase<TStateManager, NoContext>, IStateBackgroundReaction
        where TStateManager : IStateManager
{
    protected BackgroundReactionBase(TStateManager stateManager) : base(stateManager)
    {
    }

    public bool TryReact() => reactionContext.TryReact(NoContext.Instance, CanReact, InternalReact);

}

public abstract class TriggerReactionBase<TStateManager> : ReactionBase<TStateManager, Collider>, IStateReactionTrigger
        where TStateManager : IStateManager
{
    protected TriggerReactionBase(TStateManager stateManager) : base(stateManager)
    {
    }

    public bool TryReact(Collider collider) => reactionContext.TryReact(collider, CanReact, InternalReact);
}

public abstract class EventReactionBase<TStateManager> : ReactionBase<TStateManager, EnumerationReactionArgs>, IStateReactionEvent
        where TStateManager : IStateManager
{
    public Enumeration EventKey { get; }

    protected EventReactionBase(TStateManager stateManager) : base(stateManager)
    {
    }

    public override bool CanReact()
    {
        return base.CanReact() && reactionContext.Context.Enumeration == EventKey;
    }

    public bool TryReact(EnumerationReactionArgs args) => reactionContext.TryReact(args, CanReact, InternalReact);
}


