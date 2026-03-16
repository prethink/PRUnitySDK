public interface IExecutionToken<T>
    where T : WorkflowBase<T>
{
    IWorkflowPosition<T> Position { get; }

    void MoveTo(IWorkflowPosition<T> newPosition);
}