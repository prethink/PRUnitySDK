using System;

public interface IStateManager 
{
    Enumeration CurrentStateKey { get; }

    Enumeration GetDefaultStateKey();

    void SetDefaultState();

    bool IsWork();

    bool IsCurrentState(Enumeration stateKey);

    bool IsCurrentState(Type type);

    Enumeration SetState(Enumeration statekey);
    Enumeration SetState(IStateBase state);

    bool TryGetState<TState>(out TState state) where TState : class;
}
