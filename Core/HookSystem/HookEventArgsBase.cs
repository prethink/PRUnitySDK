using System.Collections.Generic;

/// <summary>
/// Базовый изменяемый контекст hook-вызова.
/// </summary>
public abstract class HookEventArgsBase
{
    private readonly List<IHookListener> modifiers = new();

    /// <summary>
    /// Наиболее приоритетный результат, установленный listeners.
    /// </summary>
    public HookResult Result { get; private set; } = HookResult.Ignored;

    /// <summary>
    /// Нужно ли вызывать оригинальное действие после выполнения pre-hooks.
    /// </summary>
    public bool ShouldCallOriginal => Result != HookResult.Supercede;

    /// <summary>
    /// Должен ли итоговый результат браться из hook-контекста.
    /// </summary>
    public bool HasResultOverride => Result >= HookResult.Override;

    /// <summary>
    /// Признак того, что хотя бы один listener изменил контекст.
    /// </summary>
    public bool IsModified => modifiers.Count > 0;

    /// <summary>
    /// Признак остановки текущей цепочки listeners.
    /// Не влияет на решение о вызове оригинального действия.
    /// </summary>
    public bool IsPropagationStopped { get; private set; }

    /// <summary>
    /// Listener, установивший текущий наиболее приоритетный результат.
    /// </summary>
    public IHookListener ResultBy { get; private set; }

    /// <summary>
    /// Listeners, изменившие hook-контекст.
    /// </summary>
    public IReadOnlyList<IHookListener> Modifiers => modifiers;

    /// <summary>
    /// Отмечает изменение контекста, не запрещая оригинальное действие.
    /// </summary>
    public virtual void Modify(IHookListener listener)
    {
        AddModifier(listener);
        PromoteResult(HookResult.Handled, listener);
    }

    /// <summary>
    /// Отмечает событие как обработанное, не запрещая оригинальное действие.
    /// </summary>
    public void Handle(IHookListener listener)
    {
        PromoteResult(HookResult.Handled, listener);
    }

    /// <summary>
    /// Сохраняет оригинальный вызов, но указывает использовать результат hook-контекста.
    /// </summary>
    public void Override(IHookListener listener)
    {
        AddModifier(listener);
        PromoteResult(HookResult.Override, listener);
    }

    /// <summary>
    /// Запрещает оригинальный вызов. Последующие listeners продолжат обработку.
    /// </summary>
    public void Supercede(IHookListener listener)
    {
        AddModifier(listener);
        PromoteResult(HookResult.Supercede, listener);
    }

    /// <summary>
    /// Останавливает вызов последующих listeners текущей стадии.
    /// </summary>
    public void StopPropagation()
    {
        IsPropagationStopped = true;
    }

    private void AddModifier(IHookListener listener)
    {
        if (listener != null && !modifiers.Contains(listener))
            modifiers.Add(listener);
    }

    private void PromoteResult(HookResult result, IHookListener listener)
    {
        if (result < Result)
            return;

        Result = result;
        ResultBy = listener;
    }
}
