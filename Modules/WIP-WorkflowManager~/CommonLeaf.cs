public class CommonLeaf<T> : ILeafNode<T>
    where T : WorkflowBase<T>
{
    public INode<T> Target { get; }

    public string Name { get; }

    public T Workflow { get; }

    public virtual void Enter(INode<T> parent)
    {
        
    }

    public virtual void Exit()
    {
        Target.Enter(this);
    }

    public virtual void PRTick()
    {

    }

    public CommonLeaf(T workflowManager, INode<T> target, string name)
    {
        this.Workflow = workflowManager;
        this.Target = target;
        this.Name = name;
    }
}
