using UnityEngine;

/// <summary>
/// Базовый объект действия.
/// Может быть клик по ссылке, загрузка сцены, или что-то другое.
/// </summary>
public abstract class ActionBase : ScriptableObject, IAction
{
    protected ActionExecuter executer = new();

    /// <summary>
    /// Выполнить действие.
    /// </summary>
    public virtual bool CanExecute()
    {
        return executer.CanExecute();
    }

    /// <summary>
    /// Можно ли выполнить действие.
    /// </summary>
    /// <returns></returns>

    public virtual bool Execute()
    {
        return executer.Execute(() => Action());
    }

    /// <summary>
    /// Само действие.
    /// </summary>
    protected abstract void Action();
}
