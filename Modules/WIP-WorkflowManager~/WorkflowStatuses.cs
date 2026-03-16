/// <summary>
/// Статусы выполнения рабочего процесса.
/// </summary>
public class WorkflowStatuses 
{
    /// <summary>
    /// Создан.
    /// </summary>
    public static readonly Enumeration Created = new("Created");   

    /// <summary>
    /// Инициализация.
    /// </summary>
    public static readonly Enumeration Starting = new("Starting");

    /// <summary>
    /// Выполняется.
    /// </summary>
    public static readonly Enumeration Running = new("Running");

    /// <summary>
    /// Приостановленный.
    /// </summary>
    public static readonly Enumeration Suspended = new("Suspended");

    /// <summary>
    /// Ожидает события.
    /// </summary>
    public static readonly Enumeration Waiting = new("Waiting");
    
    /// <summary>
    /// Завершает работу.
    /// </summary>
    public static readonly Enumeration Completing = new("Completing");

    /// <summary>
    /// Успешно завершен.
    /// </summary>
    public static readonly Enumeration Completed = new("Completed");

    /// <summary>
    /// Отменяется.
    /// </summary>
    public static readonly Enumeration Canceling = new("Canceling");
    
    /// <summary>
    /// Отменен.
    /// </summary>
    public static readonly Enumeration Canceled = new("Canceled");

    /// <summary>
    /// Ошибка.
    /// </summary>
    public static readonly Enumeration Faulted = new("Faulted");
    
    /// <summary>
    /// Прерван.
    /// </summary>
    public static readonly Enumeration Terminated = new("Terminated");
}
