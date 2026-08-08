using UnityEngine;

/// <summary>
/// Базовый объект действия.
/// Может быть клик по ссылке, загрузка сцены, или что-то другое.
/// </summary>
public abstract class ActionBase : ScriptableObject, IAction
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
    /// <returns>True, если действие было вызвано.</returns>
    public virtual bool Execute()
    {
        return executer.Execute(CanExecute, Action);
    }

    /// <summary>
    /// Реализация действия без дополнительных проверок.
    /// </summary>
    protected abstract void Action();
}
