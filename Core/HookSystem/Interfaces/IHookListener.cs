/// <summary>
/// Базовый listener системы hooks.
/// </summary>
public interface IHookListener
{
    /// <summary>
    /// Порядок выполнения. Listener с меньшим значением вызывается раньше.
    /// При одинаковом значении сохраняется порядок регистрации.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Регистрирует listener в HookManager.
    /// </summary>
    void RegisterHook();

    /// <summary>
    /// Удаляет listener из HookManager.
    /// </summary>
    void UnRegisterHook();
}

/// <summary>
/// Listener, вызываемый перед оригинальным действием.
/// </summary>
public interface IHookListener<in TArgs> : IHookListener
    where TArgs : HookEventArgsBase
{
    /// <summary>
    /// Обрабатывает pre-hook.
    /// </summary>
    void HandleHook(TArgs eventArgs);
}

/// <summary>
/// Listener, вызываемый после оригинального действия или после его блокировки через Supercede.
/// </summary>
public interface IHookPostListener<in TArgs> : IHookListener
    where TArgs : HookEventArgsBase
{
    /// <summary>
    /// Обрабатывает post-hook.
    /// </summary>
    void HandlePostHook(TArgs eventArgs);
}
