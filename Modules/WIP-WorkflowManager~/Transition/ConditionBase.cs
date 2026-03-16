using System.Collections.Generic;

public abstract class ConditionBase<T> : ITransitionCondition<T>
    where T : WorkflowBase<T>
{
    public IEnumerable<ICondition<T>> Conditions { get; protected set; }

    public T Workflow { get; }

    public int Priority { get; }

    public abstract bool Evaluate();

    protected ConditionBase(T manager, int priority)
    {
        this.Workflow = manager;
        this.Priority = priority;
    }
}
