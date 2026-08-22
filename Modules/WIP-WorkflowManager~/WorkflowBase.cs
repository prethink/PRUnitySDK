public abstract class WorkflowBase<TContext> : WorkflowBase
    where TContext : WorkflowContextBase
{
    /// <summary>
    /// Текущее контекст.
    /// </summary>
    public TContext Context { get; protected set; }
}

public abstract class WorkflowBase
{
    #region Поля и свойства

    /// <summary>
    /// Статус процесса.
    /// </summary>
    public Enumeration Status { get; protected set; } = WorkflowStatuses.Created;

    /// <summary>
    /// Текущее состояние.
    /// </summary>
    public IWorkflowPosition CurrentPosition { get; protected set; }

    #endregion

    #region Методы

    /// <summary>
    /// 
    /// </summary>
    public abstract void Start();

    /// <summary>
    /// 
    /// </summary>
    public abstract void Stop();

    #endregion
}