using UnityEngine;

/// <summary>
/// Обёртка, позволяющая повесить обычный BaseState&lt;T&gt; как компонент на
/// GameObject - чтобы получать реальные Unity-колбэки (OnTriggerEnter и т.п.)
/// напрямую от физики, без необходимости, чтобы StateManager их форвардил.
/// Сама логика стейта пишется один раз в наследнике BaseState&lt;T&gt; (Inner) -
/// эта обёртка ничего не реализует сама, только пробрасывает вызовы.
/// </summary>
public abstract class MonoBehaviourStateBase<T> : MonoBehaviour, IStateBase<T>
    where T : IStateManager
{
    /// <summary>Реальная логика стейта. Наследник задаёт конкретный тип через CreateInner().</summary>
    protected BaseState<T> Inner { get; private set; }

    /// <summary>Наследник создаёт и возвращает свой экземпляр BaseState&lt;T&gt;-логики.</summary>
    protected abstract BaseState<T> CreateInner();

    protected virtual void Awake()
    {
        Inner = CreateInner();
    }

    // === Пробрасываем всё через Inner - никакой отдельной логики здесь нет ===

    public Enumeration StateKey => Inner.StateKey;
    public bool IsStartState => Inner.IsStartState;
    public T StateManager => Inner.StateManager;

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

    public void LinkToStateManager(T stateManager) => Inner.LinkToStateManager(stateManager);
    public void OnStateTriggerEnter(Collider other) => Inner.OnStateTriggerEnter(other);
    public void OnStateTriggerStay(Collider other) => Inner.OnStateTriggerStay(other);
    public void OnStateTriggerExit(Collider other) => Inner.OnStateTriggerExit(other);
}