/// <summary>
/// Слушатель хуков.
/// </summary>
public interface IHookListener 
{
    /// <summary>
    /// Приоритет выполнения.
    /// Чем ниже приоритет, тем раньше вызывается хук.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Регистрация слушателя хуков.
    /// </summary>
    void RegisterHook();

    /// <summary>
    /// Удаление слушателя хуков.
    /// </summary>
    void UnRegisterHook();
}

/// <summary>
/// Слушатель хуков с типом аргументов.
/// </summary>
/// <typeparam name="TArgs">Тип аргумента.</typeparam>
public interface IHookListener<TArgs> : IHookListener 
    where TArgs : HookEventArgsBase
{
    /// <summary>
    /// Обработка хук.
    /// </summary>
    /// <param name="eventArgs">Аргументы события.</param>
    void HandleHook(TArgs eventArgs);
}
