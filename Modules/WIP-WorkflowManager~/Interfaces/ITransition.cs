using System.Collections.Generic;

public interface ITransition<T> : IWorkflowPosition<T>
    where T : WorkflowBase<T>
{
    int Priority { get; }

    ILeafNode<T> Target { get; }

    IReadOnlyList<ITransitionCondition<T>> Conditions { get; }

    bool CanTransition();
}
