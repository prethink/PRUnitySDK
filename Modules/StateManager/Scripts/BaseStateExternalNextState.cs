using System;

public abstract class BaseStateExternalNextState<T> : BaseState<T>
    where T : StateManagerBase<T>
{
    protected Func<Enumeration> getNextStateFunc;

    public override Enumeration GetNextState()
    {
        return getNextStateFunc();
    }

    protected BaseStateExternalNextState(Func<Enumeration> getNextStateFunc)
    {
        this.getNextStateFunc = getNextStateFunc;
    }
}
