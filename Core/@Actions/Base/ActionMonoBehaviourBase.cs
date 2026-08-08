public abstract class ActionMonoBehaviourBase : PRMonoBehaviour, IAction
{
    protected readonly ActionExecuter executer = new();
    public virtual bool CanExecute()
    {
        return executer.CanExecute();
    }

    public virtual bool Execute()
    {
        return executer.Execute(() => Action());
    }

    /// <summary>
    /// Само действие.
    /// </summary>
    protected abstract void Action();
}
