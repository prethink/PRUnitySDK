using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Менеджер хуков.
/// </summary>
public class HookManager : SingletonProviderBase<HookManager>
{
    #region Поля и свойства

    /// <summary>
    /// Зарегистрированные хуки.
    /// </summary>
    private readonly HashSet<IHookListener> hooks = new();

    #endregion

    #region Методы

    /// <summary>
    /// Опубликовать хук.
    /// </summary>
    /// <typeparam name="TArgs">Тип аргументы хука.</typeparam>
    /// <param name="hookArgs">Аргументы хука.</param>
    /// <returns>Обработанные аргументы хука.</returns>
    public TArgs Publish<TArgs>(TArgs hookArgs)
        where TArgs : HookEventArgsBase
    {
        foreach (var hook in hooks.OrderBy(x => x.Order))
        {
            if (hook is IHookListener<TArgs> instanceHook)
            {
                instanceHook.HandleHook(hookArgs);
                if (hookArgs.IsComplete)
                    break;
            }
        }
        return hookArgs;
    }

    /// <summary>
    /// Регистрация слушателя хуков.
    /// </summary>
    /// <param name="listener">Слушатель хука.</param>
    public void Register(IHookListener listener)
    {
        hooks.Add(listener);
    }

    /// <summary>
    /// Удаление слушателя хуков.
    /// </summary>
    /// <param name="listener">Слушатель хука.</param>
    public void UnRegister(IHookListener listener)
    {
        hooks.Remove(listener);
    }

    /// <summary>
    /// Есть ли зарегистрированный слушатель хука данного типа.
    /// </summary>
    /// <typeparam name="T">Тип хука.</typeparam>
    /// <returns>True - если есть, False - если нет.</returns>
    public bool HasListener<T>() 
        where T : IHookListener
    {
        return hooks.OfType<T>().Any();
    }

    #endregion
}
