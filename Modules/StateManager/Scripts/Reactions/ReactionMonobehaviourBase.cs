public abstract class ReactionMonobehaviourBase<TStateManager> : PRMonoBehaviour, IStateReaction<TStateManager> 
    where TStateManager : IStateManager
{
    public TStateManager stateManager { get; protected set; }

    protected bool isInitialize;

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

    public bool TryReact()
    {
        if(!CanReact()) 
            return false;

        InternalReact();
        return true;
    }

    protected abstract void InternalReact();
}

