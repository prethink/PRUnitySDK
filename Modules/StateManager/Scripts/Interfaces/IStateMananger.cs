public interface IStateManager 
{
    Enumeration CurrentStateKey { get; }

    Enumeration GetDefaultStateKey();

    void SetDefaultState();

    bool IsWork();

    bool IsCurrentState(Enumeration stateKey);

    Enumeration SetState(Enumeration statekey);
    Enumeration SetState(IBaseState state);

    bool TryGetState<TState>(out TState state) where TState : class;
}
