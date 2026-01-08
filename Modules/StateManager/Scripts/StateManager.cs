using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Менеджер состояний.
/// </summary>
public class StateManager : PRMonoBehaviour
{
    #region Поля и свойства

    /// <summary>
    /// Состояния.
    /// </summary>
    protected Dictionary<string, IBaseState> States = new();

    /// <summary>
    /// Текущее состояние.
    /// </summary>
    public IBaseState CurrentState { get; protected set; }

    /// <summary>
    /// Предыдущее состояние.
    /// </summary>
    public IBaseState PreviousState { get; protected set; }

    /// <summary>
    /// Признак того, что происходит переход между состояниями.
    /// </summary>
    protected bool IsTransitionState;

    /// <summary>
    /// Признак того, что state machine работает.
    /// </summary>
    protected bool isWork;

    /// <summary>
    /// Ключ текущего состояния. Используется для отладки.
    /// </summary>
    [field: SerializeField] public string CurrentStateKey { get; protected set; }

    #endregion

    #region События

    public event Action<string> OnChangeState;

    #endregion

    /// <summary>
    /// Запустить машину состояний.
    /// </summary>
    private void StartStateMachine()
    {
        CurrentState = States.Single(x => x.Value.IsStartState == true).Value;

        if (CurrentState == null)
            throw new NullReferenceException($"{nameof(CurrentState)} is null.");

        CurrentState.EnterState();
        CurrentStateKey = CurrentState.StateKey;
        isWork = true;
    }

    /// <summary>
    /// Инициализация машины состояний.
    /// </summary>
    protected virtual void InitStateMachine()
    {
        var monoBehaviourStates = GetComponents<IBaseState>();
        foreach (var monoBehaviourState in monoBehaviourStates)
        {
            States.Add(monoBehaviourState.StateKey, monoBehaviourState);

            if (monoBehaviourState.IsStartState)
                CurrentState = monoBehaviourState;

            monoBehaviourState.LinkToStateManager(this);
        }
        StartStateMachine();
    }

    #region MonoBehaviour

    protected override void PRUpdate()
    {
        if (!IsWork())
            return;

        PreUpdate();

        string nextStateKey = CurrentState.GetNextState();

        if (nextStateKey.Equals(CurrentState.StateKey))
            CurrentState.UpdateState();
        else
            TransitionToState(nextStateKey);

        PostUpdate();
    }

    #endregion

    public virtual bool IsWork()
    {
        return isWork;
    }

    protected virtual void PreUpdate()
    {

    }

    protected virtual void PostUpdate()
    {

    }

    public void AddState(IBaseState state)
    {
        States.Add(state.StateKey, state);
    }

    /// <summary>
    /// Совпадает ли текущее состояние с указаным ключем.
    /// </summary>
    /// <param name="stateKey">Ключ состояния.</param>
    /// <returns>True - совпадает, False - нет.</returns>
    public bool IsCurrentState(string stateKey)
    {
        return CurrentState?.StateKey.Equals(stateKey, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Установить новое состояние.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    public string SetState(string statekey)
    {
        if(States.ContainsKey(statekey))
            return TransitionToState(statekey);
        else
            throw new Exception($"В коллекции состояний отсутствует - {statekey}");
    }

    /// <summary>
    /// Переход на следующее состояние.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    private string TransitionToState(string statekey)
    {
        IsTransitionState = true;
        CurrentState.ExitState();
        PreviousState = CurrentState;
        CurrentState = States[statekey];
        CurrentStateKey = statekey;
        NotifyStateChange(statekey);
        CurrentState.EnterState();
        IsTransitionState = false;
        return statekey;

    }

    /// <summary>
    /// Оповестить об изменение состояния.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    public void NotifyStateChange(string statekey)
    {
        OnChangeState?.Invoke(statekey);
    }

    protected override void PROnTriggerEnter(Collider other)
    {
        if (CurrentState is BaseState)
            CurrentState.OnTriggerEnter(other);

    }

    protected override void PROnTriggerStay(Collider other)
    {
        if (CurrentState is BaseState)
            CurrentState.OnTriggerStay(other);
    }

    protected override void PROnTriggerExit(Collider other)
    {
        if (CurrentState is BaseState)
            CurrentState.OnTriggerExit(other);
    }


    #region AnimationTrigger
    public virtual void AnimationTrigger() 
    { 
        CurrentState?.AnimationTrigger();
    }

    public virtual void AnimationTriggerFloat(float data)
    {
        CurrentState?.AnimationTriggerFloat(data);
    }

    public virtual void AnimationTriggerInt(int data)
    {
        CurrentState?.AnimationTriggerInt(data);
    }

    public virtual void AnimationTriggerString(string data)
    {
        CurrentState?.AnimationTriggerString(data);
    }

    public virtual void AnimationTriggerGameObject(GameObject data)
    {
        CurrentState?.AnimationTriggerGameObject(data);
    }
    #endregion
}
