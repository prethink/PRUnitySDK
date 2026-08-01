using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Менеджер состояний.
/// </summary>
public abstract class StateManagerBase<T> : PRMonoBehaviour , IStateManager
    where T : StateManagerBase<T>
{
    #region Поля и свойства

    /// <summary>
    /// Состояния.
    /// </summary>
    protected Dictionary<Enumeration, IBaseState<T>> states = new();

    /// <summary>
    /// Состояние с возможностью вызова в случайный промежуток времени.
    /// </summary>
    protected HashSet<IRandomState> randomStates = new();

    /// <summary>
    /// Реакции.
    /// </summary>
    protected HashSet<IStateReaction> reactions = new();

    /// <summary>
    /// Реакции.
    /// </summary>
    protected HashSet<IStateBackgroundReaction> backgroundReactions = new();

    /// <summary>
    /// 
    /// </summary>
    protected HashSet<IStateReactionTrigger> triggerReactions = new();

    /// <summary>
    /// Признак того, что 
    /// </summary>
    public bool СanTryExecuteRandomState { get; protected set; }

    /// <summary>
    /// Текущее состояние.
    /// </summary>
    public IBaseState<T> CurrentState { get; protected set; }

    /// <summary>
    /// Предыдущее состояние.
    /// </summary>
    public IBaseState<T> PreviousState { get; protected set; }

    /// <summary>
    /// Интервал тика (в секундах). Если 0 — не использовать Tick.
    /// </summary>
    public abstract float Tick { get; }

    /// <summary>
    /// Текущий тик.
    /// </summary>
    private CooldownBase Cooldown = new CooldownGameTime();

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
    [field: SerializeField] public Enumeration CurrentStateKey { get; protected set; }

    protected string debugCurrentState;
    protected List<string> debugReactionsNames = new();
    protected List<string> debugStatesNames = new();

    #endregion

    #region События

    public event Action<Enumeration> OnChangeState;

    #endregion

    /// <summary>
    /// Запустить машину состояний.
    /// </summary>
    private void StartStateMachine()
    {
        CurrentState = states.Single(x => x.Value.IsStartState == true).Value;

        if (CurrentState == null)
            throw new NullReferenceException($"{nameof(CurrentState)} is null.");

        CurrentState.EnterState();
        CurrentStateKey = CurrentState.StateKey;
        StartWork();
    }

    /// <summary>
    /// Инициализация машины состояний.
    /// </summary>
    protected virtual void InitStateMachine()
    {
        debugStatesNames.Clear();
        debugReactionsNames.Clear();

        RegisterMonoBehaviourStates();
        RegisterStates();
        RegisterReactions();

        foreach (var state in states)
            state.Value.LinkToStateManager(this as T);

        StartStateMachine();
    }

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        InitializeStateManager();
    }

    protected abstract void InitializeStateManager();

    public Enumeration GetDefaultStateKey()
    {
        return states.Single(x => x.Value.IsStartState).Key;
    }

    protected virtual void RegisterMonoBehaviourStates()
    {
        var monoBehaviourStates = GetComponents<IBaseState<T>>();
        foreach (var monoBehaviourState in monoBehaviourStates)
            RegisterState(monoBehaviourState);
    }

    protected virtual void RegisterReactions()
    {
        var reactions = GetComponents<IStateReaction>();
        foreach (var reaction in reactions)
            RegisterReaction(reaction);
    }

    protected void RegisterReaction(IStateReaction reaction)
    {
        if (reaction is IStateReaction<T> typedReaction)
        {
            typedReaction.Initialize(this as T);
            reactions.Add(reaction);
            debugReactionsNames.Add(reaction.GetType().Name);
        }

        if (reaction is IStateBackgroundReaction backgroundReaction)
            backgroundReactions.Add(backgroundReaction);

        if(reaction is IStateReactionTrigger triggerReaction)
            triggerReactions.Add(triggerReaction);
    }

    protected virtual void RegisterState(IBaseState<T> state)
    {
        states.Add(state.StateKey, state);

        var startStates = states.Where(x => x.Value.IsStartState);

        if (startStates.Count() > 1)
        {
            var startStatesNames = startStates.Select(x => x.Key.Value);
            throw new InvalidOperationException($"More one states with property {nameof(IBaseState.IsStartState)}. States {string.Join(',', startStatesNames)}");
        }

        if (state.IsStartState)
        {
            CurrentState = state;
            PreviousState = CurrentState;
        }

        if(state is IRandomState randomState)
            randomStates.Add(randomState);

        debugStatesNames.Add(state.GetType().Name);
    }

    protected abstract void RegisterStates();

    public virtual void SetDefaultState()
    {
        var defaultState = states.Single(States => States.Value.IsStartState);
        SetState(defaultState.Key);
    }

    public void SetСanTryExecuteRandomState()
    {
        СanTryExecuteRandomState = true;
    }

    public void BlockСanTryExecuteRandomState()
    {
        СanTryExecuteRandomState = false;
    }

    public virtual void StopWork(bool setDefaultState = false)
    {
        isWork = false;

        if(setDefaultState)
            SetDefaultState();
    }

    public virtual void StartWork()
    {
        isWork = true;
    }

    #region MonoBehaviour

    protected override void PRUpdate()
    {
        if (!IsWork())
            return;

        PreUpdate();

        foreach (var state in states)
            state.Value.BackgroundUpdate();

        UpdateStateManager();

        var nextStateKey = CurrentState.GetNextState();

        if (nextStateKey.Equals(CurrentState.StateKey))
        {
            Cooldown.TryExecute(Tick, () =>
            {
                TickRate();
            });

            CurrentState.UpdateState();
        }
        else
            TransitionToState(nextStateKey);

        PostUpdate();
    }

    protected virtual void UpdateStateManager()
    {

    }

    protected virtual void TickRate()
    {
        CurrentState.Tick();

        foreach (var reaction in backgroundReactions)
        {
            if (reaction.TryReact())
                break;
        }

        if(СanTryExecuteRandomState && randomStates.Any())
        {
            foreach (var state in randomStates)
            {
                if (state.TryRandomStateTrigger())
                    break;
            }
        }
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

    protected void AddState(IBaseState<T> state)
    {
        states.Add(state.StateKey, state);
    }

    /// <summary>
    /// Совпадает ли текущее состояние с указаным ключем.
    /// </summary>
    /// <param name="stateKey">Ключ состояния.</param>
    /// <returns>True - совпадает, False - нет.</returns>
    public bool IsCurrentState(Enumeration stateKey)
    {
        return CurrentState?.StateKey.Equals(stateKey) == true;
    }

    public bool IsCurrentState(Type type)
    {
        return CurrentState?.StateKey.GetType().Equals(type) == true;
    }

    /// <summary>
    /// Установить новое состояние.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    public Enumeration SetState(Enumeration statekey)
    {
        if(states.ContainsKey(statekey))
            return TransitionToState(statekey);
        else
            throw new Exception($"В коллекции состояний отсутствует - {statekey}");
    }

    public Enumeration SetState(IBaseState state)
    {
        return SetState(state.StateKey);
    }

    /// <summary>
    /// Переход на следующее состояние.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    private Enumeration TransitionToState(Enumeration statekey)
    {
        IsTransitionState = true;
        СanTryExecuteRandomState = false;
        CurrentState.ExitState();
        PreviousState = CurrentState;
        CurrentState = states[statekey];
        CurrentStateKey = statekey;
        debugCurrentState = CurrentStateKey.Value;
        NotifyStateChange(statekey);
        CurrentState.EnterState();
        IsTransitionState = false;
        return statekey;
    }

    /// <summary>
    /// Оповестить об изменение состояния.
    /// </summary>
    /// <param name="statekey">Ключ состояния.</param>
    protected void NotifyStateChange(Enumeration statekey)
    {
        OnChangeState?.Invoke(statekey);
    }

    protected override void PROnTriggerEnter(Collider other)
    {
        CurrentState?.OnTriggerEnter(other);
    }

    protected override void PROnTriggerStay(Collider other)
    {
        CurrentState?.OnTriggerStay(other);
    }

    protected override void PROnTriggerExit(Collider other)
    {
        CurrentState?.OnTriggerExit(other);
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

    public bool TryGetState<TState>(out TState state) where TState : class
    {
        state = states.Values.OfType<TState>().FirstOrDefault();
        return state != null;
    }

    #endregion
}
