using System;

/// <summary>
/// Действие, которое настраивается прямо в инспекторе владельца и не требует ассета.
/// </summary>
/// <remarks>
/// От <see cref="ActionBase"/> отличается только способом хранения: то же разделение
/// <see cref="CanExecute"/> и <see cref="Action"/>, тот же <see cref="ActionExecuter"/>.
/// Ассет удобен, когда настройка переиспользуется в разных местах, встроенное действие —
/// когда оно уникально для конкретного объекта.
/// <para>
/// Поле владельца объявляется так:
/// <code>[SerializeReference, ReferenceSelector] private IAction action;</code>
/// </para>
/// </remarks>
[Serializable]
public abstract class InlineActionBase : IAction
{
    /// <summary>
    /// Общий executor проверки и выполнения.
    /// </summary>
    /// <remarks>
    /// Не сериализуется и создаётся лениво: Unity восстанавливает объект из данных,
    /// минуя конструктор, поэтому поле может оказаться пустым после загрузки сцены.
    /// </remarks>
    private ActionExecuter executer;

    /// <summary>
    /// Executor, гарантированно готовый к работе.
    /// </summary>
    protected ActionExecuter Executer => executer ??= new ActionExecuter();

    /// <summary>
    /// Проверяет возможность выполнения действия.
    /// </summary>
    public virtual ActionResult CanExecute()
    {
        return Executer.CanExecute();
    }

    /// <summary>
    /// Выполняет действие, если <see cref="CanExecute"/> возвращает успешный результат.
    /// </summary>
    /// <returns>Результат проверки или выполнения действия.</returns>
    public virtual ActionResult Execute()
    {
        return Executer.Execute(CanExecute, Action);
    }

    /// <summary>
    /// Реализация действия без дополнительных проверок.
    /// </summary>
    protected abstract ActionResult Action();
}
