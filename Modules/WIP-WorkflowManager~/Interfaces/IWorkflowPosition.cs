public interface IWorkflowPosition<T> : IWorkflowPosition, IWorkflowManagerProvider<T>
     where T : WorkflowBase<T>
{

}

public interface IWorkflowPosition : INameProvider, IPRTickable
{

}
