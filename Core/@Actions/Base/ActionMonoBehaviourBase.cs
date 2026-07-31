public abstract class ActionMonoBehaviourBase : PRMonoBehaviour, IAction
{
    public virtual bool CanExecute()
    {
        if (!PRUnitySDK.IsInitialized)
        {
            PRLog.WriteWarning(GetType(), $"Can't execute action, SDK not initialized.");
            return false;
        }

        return true;
    }

    public virtual bool Execute()
    {
        if (!CanExecute())
            return false;

        Action();
        return true;
    }

    /// <summary>
    /// Само действие.
    /// </summary>
    protected abstract void Action();
}
