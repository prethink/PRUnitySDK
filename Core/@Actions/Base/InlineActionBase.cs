using System;

/// <summary>
/// Действие, которое настраивается прямо в инспекторе владельца и не требует ассета.
/// </summary>
/// <remarks>
/// Отличается от <see cref="ActionBase"/> способом хранения, а не смыслом: то же
/// разделение <see cref="CanExecute"/> и <see cref="Action"/>, тот же
/// <see cref="ActionExecuter"/>. Ассет-действие удобно, когда одна и та же настройка
/// переиспользуется в разных местах; встроенное - когда действие уникально для
/// конкретного объекта и заводить под него отдельный файл незачем.
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
    public virtual bool CanExecute()
    {
        return Executer.CanExecute();
    }

    /// <summary>
    /// Выполняет действие, если <see cref="CanExecute"/> возвращает true.
    /// </summary>
    /// <returns>True, если действие было вызвано.</returns>
    public virtual bool Execute()
    {
        return Executer.Execute(CanExecute, Action);
    }

    /// <summary>
    /// Реализация действия без дополнительных проверок.
    /// </summary>
    protected abstract void Action();
}
