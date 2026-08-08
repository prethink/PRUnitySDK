using System;

public class ActionExecuter 
{
    public bool CanExecute()
    {
        if (!PRUnitySDK.IsInitialized)
        {
            PRLog.WriteWarning(GetType(), $"Can't execute action, SDK not initialized.");
            return false;
        }

        return true;
    }

    public bool Execute(Action action)
    {
        if (!CanExecute())
            return false;

        action();
        return true;
    }
}
