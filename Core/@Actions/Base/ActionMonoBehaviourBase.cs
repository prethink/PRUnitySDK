/// <summary>
/// Базовый MonoBehaviour для действия, связанного с конкретным GameObject.
/// </summary>
public abstract class ActionMonoBehaviourBase : PRMonoBehaviour, IAction
{
    /// <summary>
    /// Общий executor проверки и выполнения.
    /// </summary>
    protected readonly ActionExecuter executer = new();

    /// <summary>
    /// Проверяет возможность выполнения действия.
    /// </summary>
    public virtual bool CanExecute()
    {
        return executer.CanExecute();
    }

    /// <summary>
    /// Выполняет действие, если CanExecute() возвращает true.
    /// </summary>
    public virtual bool Execute()
    {
        return executer.Execute(CanExecute, Action);
    }

    /// <summary>
    /// Реализация действия без дополнительных проверок.
    /// </summary>
    protected abstract void Action();
}
