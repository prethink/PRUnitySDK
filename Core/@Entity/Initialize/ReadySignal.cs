using System;
using System.Collections.Generic;

/// <summary>
/// Реализация сигнала готовности с защитой от дубликатов подписок.
/// </summary>
public class ReadySignal : IReadySignal
{
    private readonly object _lock = new object();
    private readonly HashSet<Action> onReadyCallbacks = new();
    private volatile bool isReady;

    /// <summary>
    /// Получает значение, указывающее, был ли сигнал помечен как готовый.
    /// </summary>
    public bool IsReady => isReady;

    /// <summary>
    /// Имя сигнала.
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// Помечает сигнал как готовый и вызывает все подписанные callback'и.
    /// </summary>
    /// <remarks>
    /// Если уже помечен как готовый, метод не имеет эффекта.
    /// После вызова всех callback'ов, они очищаются для предотвращения утечек памяти.
    /// </remarks>
    public void SetReady()
    {
        Action[] callbacks;

        lock (_lock)
        {
            if (IsReady)
                return;

            isReady = true;
            callbacks = new Action[onReadyCallbacks.Count];
            onReadyCallbacks.CopyTo(callbacks);
            onReadyCallbacks.Clear();
        }

        try
        {
            ReadySignalEvents.RaiseReadySignal(Name);
        }
        catch (Exception exception)
        {
            PRLog.WriteError(this, exception.ToString());
        }

        foreach (var callback in callbacks)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception exception)
            {
                PRLog.WriteError(this, exception.ToString());
            }
        }
    }

    /// <summary>
    /// Подписывает callback на событие готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для вызова при готовности. Если уже готово, вызывается немедленно.</param>
    /// <returns>Возвращает этот экземпляр для цепочки методов.</returns>
    /// <remarks>
    /// Попытка подписать один и тот же callback дважды игнорируется с предупреждением.
    /// </remarks>
    public IReadySignal SubscribeOnReady(Action onReadyCallback)
    {
        if (onReadyCallback == null)
            return this;

        bool invokeImmediately;

        lock (_lock)
        {
            invokeImmediately = IsReady;
            if (!invokeImmediately)
                onReadyCallbacks.Add(onReadyCallback);
        }

        if (invokeImmediately)
            onReadyCallback.Invoke();

        return this;
    }

    /// <summary>
    /// Отписывает callback от события готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для удаления из списка подписок.</param>
    /// <remarks>
    /// Если callback не подписан, метод не имеет эффекта.
    /// </remarks>
    public void UnSubscribe(Action onReadyCallback)
    {
        if (onReadyCallback == null)
            return;

        lock (_lock)
        {
            onReadyCallbacks.Remove(onReadyCallback);
        }
    }

    /// <summary>
    /// Сбрасывает состояние готовности и очищает всех подписчиков.
    /// </summary>
    /// <remarks>
    /// Используйте с осторожностью, так как это очистит все подписанные callback'и без уведомления.
    /// </remarks>
    public void ResetReady()
    {
        lock (_lock)
        {
            isReady = false;
            onReadyCallbacks.Clear();
        }
    }

    /// <summary>
    /// Освобождает сигнал и очищает все ресурсы.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            isReady = false;
            onReadyCallbacks.Clear();
        }
    }

    /// <summary>
    /// Возвращает количество активных подписчиков.
    /// </summary>
    public int GetSubscribersCount()
    {
        lock (_lock)
        {
            return onReadyCallbacks.Count;
        }
    }

    public ReadySignal(string Name)
    {
        this.Name = Name;
    }

    public ReadySignal(Enumeration enumeration) : this(enumeration.Value) {}
    public ReadySignal(Type type) : this(type.Name) { }
    public ReadySignal(object obj) : this(obj.GetType().Name) { }
}
