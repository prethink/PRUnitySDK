public interface INode<T>  : IWorkflowPosition<T>
    where T : WorkflowBase<T>
{

    void Enter(ILeafNode<T> leafNode);
    void Enter();

    void Exit();

    public Enumeration Status { get; }
}
