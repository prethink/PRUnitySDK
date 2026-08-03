using UnityEngine;

public abstract class MonoStateContainerBase<T, TStateManager> : MonoBehaviour, IStateBase<TStateManager>
    where T : IStateBase<TStateManager>
    where TStateManager : IStateManager
{
    protected T Inner { get; private set; }

    protected abstract T CreateInner();

    protected virtual void Awake()
    {
        Inner = CreateInner();
    }

    public Enumeration StateKey => Inner.StateKey;
    public bool IsStartState => Inner.IsStartState;
    public TStateManager StateManager => Inner.StateManager;

    public void EnterState() => Inner.EnterState();
    public void ExitState() => Inner.ExitState();
    public void UpdateState() => Inner.UpdateState();
    public void Tick() => Inner.Tick();
    public Enumeration GetNextState() => Inner.GetNextState();
    public void BackgroundUpdate() => Inner.BackgroundUpdate();

    public void AnimationTrigger() => Inner.AnimationTrigger();
    public void AnimationTriggerFloat(float data) => Inner.AnimationTriggerFloat(data);
    public void AnimationTriggerInt(int data) => Inner.AnimationTriggerInt(data);
    public void AnimationTriggerString(string data) => Inner.AnimationTriggerString(data);
    public void AnimationTriggerGameObject(GameObject data) => Inner.AnimationTriggerGameObject(data);

    public void LinkToStateManager(TStateManager stateManager) => Inner.LinkToStateManager(stateManager);

    public void OnStateTriggerEnter(Collider other) => Inner.OnStateTriggerEnter(other);
    public void OnStateTriggerStay(Collider other) => Inner.OnStateTriggerStay(other);
    public void OnStateTriggerExit(Collider other) => Inner.OnStateTriggerExit(other);
}