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
    public virtual ActionResult CanExecute()
    {
        return executer.CanExecute();
    }

    /// <summary>
    /// Выполняет действие, если CanExecute() возвращает успешный результат.
    /// </summary>
    /// <returns>Результат проверки или выполнения действия.</returns>
    public virtual ActionResult Execute()
    {
        return executer.Execute(CanExecute, Action);
    }

    /// <summary>
    /// Реализация действия без дополнительных проверок.
    /// </summary>
    protected abstract ActionResult Action();
}
