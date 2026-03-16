public interface ICondition<T> : IWorkflowManagerProvider<T>, IPrioritized
    where T : WorkflowBase<T>
{
    bool Evaluate();
}
