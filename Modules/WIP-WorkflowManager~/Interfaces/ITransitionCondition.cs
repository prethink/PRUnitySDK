using System.Collections.Generic;

public interface ITransitionCondition<T> : ICondition<T>, IPrioritized
    where T : WorkflowBase<T>
{
    IEnumerable<ICondition<T>> Conditions { get; }
}
