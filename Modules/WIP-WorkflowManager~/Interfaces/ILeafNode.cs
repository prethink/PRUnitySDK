public interface ILeafNode<T> : IWorkflowPosition<T>
    where T : WorkflowBase<T>
{
    INode<T> Target { get; }
    void Enter(INode<T> parent);
    void Exit();
}


