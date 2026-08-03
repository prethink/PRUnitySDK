using UnityEngine;

public abstract class MonoStateBase<T> : MonoBehaviour, IStateBase<T>
    where T : IStateManager
{
    #region IBaseState


    /// <inheritdoc />
    public abstract Enumeration StateKey { get; }

    /// <inheritdoc />
    public abstract bool IsStartState { get; }

    public T StateManager { get; protected set; }

    /// <inheritdoc />
    public abstract void EnterState();

    /// <inheritdoc />
    public abstract void ExitState();

    /// <inheritdoc />
    public abstract void UpdateState();

    /// <inheritdoc />
    public virtual void BackgroundUpdate() { }

    /// <inheritdoc />
    public abstract void Tick();

    /// <inheritdoc />
    public abstract Enumeration GetNextState();

    /// <inheritdoc />
    public virtual void AnimationTrigger() { }

    /// <inheritdoc />
    public virtual void AnimationTriggerFloat(float data) { }

    /// <inheritdoc />
    public virtual void AnimationTriggerInt(int data) { }

    /// <inheritdoc />
    public virtual void AnimationTriggerString(string data) { }

    /// <inheritdoc />
    public virtual void AnimationTriggerGameObject(GameObject data) { }

    /// <inheritdoc />
    public virtual void LinkToStateManager(T stateManager)
    {
        this.StateManager = stateManager;
    }

    public virtual void OnStateTriggerEnter(Collider other) { }

    public virtual void OnStateTriggerStay(Collider other) { }

    public virtual void OnStateTriggerExit(Collider other) { }

    #endregion
}