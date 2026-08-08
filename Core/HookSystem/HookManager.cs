using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Выполняет типизированные pre- и post-hooks в стабильном порядке.
/// </summary>
public class HookManager : SingletonProviderBase<HookManager>
{
    private sealed class HookPipeline
    {
        public IHookListener[] PreHooks { get; set; }
        public IHookListener[] PostHooks { get; set; }
    }

    private readonly List<IHookListener> hooks = new();
    private readonly Dictionary<Type, HookPipeline> pipelineCache = new();

    /// <summary>
    /// Публикует контекст без оригинального действия.
    /// Подходит, когда основной код выполняется вызывающей стороной после проверки результата.
    /// </summary>
    public TArgs Publish<TArgs>(TArgs hookArgs)
        where TArgs : HookEventArgsBase
    {
        return Publish(hookArgs, null);
    }

    /// <summary>
    /// Выполняет pre-hooks, разрешённое оригинальное действие и post-hooks.
    /// Supercede запрещает оригинальное действие, но не останавливает последующие hooks.
    /// </summary>
    /// <param name="hookArgs">Изменяемый контекст вызова.</param>
    /// <param name="originalAction">Оригинальное действие, которое можно перехватить.</param>
    public TArgs Publish<TArgs>(TArgs hookArgs, Action<TArgs> originalAction)
        where TArgs : HookEventArgsBase
    {
        if (hookArgs == null)
            throw new ArgumentNullException(nameof(hookArgs));

        var pipeline = GetPipeline<TArgs>();
        InvokePreHooks(pipeline.PreHooks, hookArgs);

        if (hookArgs.ShouldCallOriginal)
            originalAction?.Invoke(hookArgs);

        if (!hookArgs.IsPropagationStopped)
            InvokePostHooks(pipeline.PostHooks, hookArgs);

        return hookArgs;
    }

    /// <summary>
    /// Регистрирует listener. Повторная регистрация того же экземпляра игнорируется.
    /// </summary>
    public void Register(IHookListener listener)
    {
        if (listener == null)
            throw new ArgumentNullException(nameof(listener));

        if (hooks.Contains(listener))
            return;

        hooks.Add(listener);
        pipelineCache.Clear();
    }

    /// <summary>
    /// Удаляет listener и возвращает признак успешного удаления.
    /// </summary>
    public bool Unregister(IHookListener listener)
    {
        if (listener == null || !hooks.Remove(listener))
            return false;

        pipelineCache.Clear();
        return true;
    }

    /// <summary>
    /// Удаляет listener. Оставлено для совместимости с прежним написанием метода.
    /// </summary>
    [Obsolete("Use Unregister instead.")]
    public void UnRegister(IHookListener listener)
    {
        Unregister(listener);
    }

    /// <summary>
    /// Проверяет наличие зарегистрированного listener указанного типа.
    /// </summary>
    public bool HasListener<T>()
        where T : IHookListener
    {
        foreach (var hook in hooks)
        {
            if (hook is T)
                return true;
        }

        return false;
    }

    private HookPipeline GetPipeline<TArgs>()
        where TArgs : HookEventArgsBase
    {
        var argsType = typeof(TArgs);
        if (pipelineCache.TryGetValue(argsType, out var pipeline))
            return pipeline;

        // OrderBy стабилен, поэтому при одинаковом Order сохраняется порядок регистрации.
        var orderedHooks = hooks.OrderBy(listener => listener.Order).ToArray();
        pipeline = new HookPipeline
        {
            PreHooks = orderedHooks.Where(listener => listener is IHookListener<TArgs>).ToArray(),
            PostHooks = orderedHooks.Where(listener => listener is IHookPostListener<TArgs>).ToArray()
        };

        pipelineCache.Add(argsType, pipeline);
        return pipeline;
    }

    private static void InvokePreHooks<TArgs>(IHookListener[] listeners, TArgs hookArgs)
        where TArgs : HookEventArgsBase
    {
        foreach (var listener in listeners)
        {
            ((IHookListener<TArgs>)listener).HandleHook(hookArgs);
            if (hookArgs.IsPropagationStopped)
                return;
        }
    }

    private static void InvokePostHooks<TArgs>(IHookListener[] listeners, TArgs hookArgs)
        where TArgs : HookEventArgsBase
    {
        foreach (var listener in listeners)
        {
            ((IHookPostListener<TArgs>)listener).HandlePostHook(hookArgs);
            if (hookArgs.IsPropagationStopped)
                return;
        }
    }
}
