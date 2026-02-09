using UnityEngine;

/// <summary>
/// Ѕазовое состо€ние использу€ компонентную систему MonoBehaviour.
/// </summary>
public abstract class MonoBehaviourStateBase<T> : MonoBehaviour, IBaseState<T> 
    where T : StateManagerBase<T>
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
    public abstract void Tick();

    /// <inheritdoc />
    public abstract Enumeration GetNextState();

    /// <inheritdoc />
    public abstract void OnTriggerEnter(Collider other);

    /// <inheritdoc />
    public abstract void OnTriggerStay(Collider other);

    /// <inheritdoc />
    public abstract void OnTriggerExit(Collider other);

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

    }

    #endregion
}