using System;

/// <summary>
/// Реализация сигнала готовности, который уведомляет подписчиков, когда операция или ресурс готовы к использованию.
/// </summary>
public class ReadySignal : IReadySignal
{
    private readonly object _lock = new object();
    private event Action _onReadyEvent;

    /// <summary>
    /// Получает значение, указывающее, был ли сигнал помечен как готовый.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// Помечает сигнал как готовый и вызывает все подписанные callback'и.
    /// </summary>
    /// <remarks>
    /// Если уже помечен как готовый, метод не имеет эффекта.
    /// После вызова всех callback'ов, событие очищается для предотвращения утечек памяти.
    /// </remarks>
    public void SetReady()
    {
        lock (_lock)
        {
            if (IsReady)
                return;

            IsReady = true;
            _onReadyEvent?.Invoke();
            _onReadyEvent = null;
        }
    }

    /// <summary>
    /// Подписывает callback на событие готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для вызова при готовности. Если уже готово, вызывается немедленно.</param>
    /// <returns>Возвращает этот экземпляр для цепочки методов.</returns>
    public IReadySignal SubscribeOnReady(Action onReadyCallback)
    {
        if (onReadyCallback == null)
            return this;

        lock (_lock)
        {
            if (IsReady)
            {
                onReadyCallback.Invoke();
                return this;
            }

            _onReadyEvent += onReadyCallback;
        }

        return this;
    }

    /// <summary>
    /// Отписывает callback от события готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для удаления из списка подписок.</param>
    public void UnSubscribe(Action onReadyCallback)
    {
        if (onReadyCallback == null)
            return;

        lock (_lock)
        {
            _onReadyEvent -= onReadyCallback;
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
            IsReady = false;
            _onReadyEvent = null;
        }
    }

    /// <summary>
    /// Освобождает сигнал и очищает все ресурсы.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            IsReady = false;
            _onReadyEvent = null;
        }
    }
}