using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Базовые аргументы события хука.
/// </summary>
public abstract class HookEventArgsBase
{
    #region Поля и свойства

    /// <summary>
    /// Результат срабатывания хука.
    /// </summary>
    public HookResult Result { get; set; } = HookResult.Continue;

    /// <summary>
    /// Признак того, что обработка хука завершена.
    /// </summary>
    public bool IsComplete => Result == HookResult.Handled || Result == HookResult.Cancel || Result == HookResult.HandledMain;

    /// <summary>
    /// Признак того, что событие было модифицировано.
    /// </summary>
    public bool IsModified => modifiers.Count > 0;

    /// <summary>
    /// Тот, кто отменил событие.
    /// </summary>
    public IHookListener CancelBy;

    /// <summary>
    /// Модификаторы, которые изменили событие.
    /// </summary>
    private List<IHookListener> modifiers = new();

    /// <summary>
    /// Модификаторы, которые изменили событие.
    /// </summary>
    public IReadOnlyList<IHookListener> Modifiers => modifiers.ToList();

    #endregion

    #region Методы

    /// <summary>
    /// Модифицировать событие.
    /// </summary>
    /// <param name="listener">Слушатель хука.</param>
    public virtual void Modify(IHookListener listener)
    {
        modifiers.Add(listener);
        Result = HookResult.Modified;
    }

    /// <summary>
    /// Отменить событие.
    /// </summary>
    /// <param name="listener">Слушатель хука.</param>
    public virtual void Cancel(IHookListener listener)
    {
        CancelBy = listener;
        Result = HookResult.Cancel;
    }

    #endregion
}