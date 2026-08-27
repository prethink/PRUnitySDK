using System.Collections.Generic;

/// <summary>
/// Фоновая задача, опрашивающая значение, которое само о себе не сообщает,
/// и уведомляющая подписчиков только в момент его изменения.
/// </summary>
/// <typeparam name="T">Тип наблюдаемого значения.</typeparam>
/// <remarks>
/// Если наблюдателю нужны ссылки на объекты сцены или настройка в инспекторе,
/// используйте <see cref="WatcherTaskBehaviour{T}"/>.
/// </remarks>
public abstract class WatcherTask<T> : BackgroundTask, IWatcherTask<T>
{
    #region Поля и свойства

    /// <summary>
    /// Наблюдаемое значение и его события.
    /// </summary>
    public WatcherState<T> Watcher { get; }

    /// <summary>
    /// Последнее прочитанное значение.
    /// </summary>
    public T CurrentValue => Watcher.CurrentValue;

    /// <summary>
    /// Значение уже было прочитано хотя бы один раз.
    /// </summary>
    public bool HasValue => Watcher.HasValue;

    /// <summary>
    /// Поднимать <see cref="Changed"/> при самом первом чтении.
    /// </summary>
    /// <remarks>
    /// Подписчику, пришедшему позже, стартовое значение доступно через
    /// <see cref="CurrentValue"/> и <see cref="HasValue"/>.
    /// </remarks>
    public virtual bool RaiseOnFirstRead => true;

    protected WatcherTask()
    {
        Watcher = new WatcherState<T>(this);
    }

    #endregion

    #region События

    /// <summary>
    /// Значение изменилось. Передаётся новое значение.
    /// </summary>
    public event System.Action<T> Changed
    {
        add => Watcher.Changed += value;
        remove => Watcher.Changed -= value;
    }

    /// <summary>
    /// Значение изменилось. Передаются предыдущее и новое значения.
    /// </summary>
    public event System.Action<T, T> ChangedWithPrevious
    {
        add => Watcher.ChangedWithPrevious += value;
        remove => Watcher.ChangedWithPrevious -= value;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Читает текущее значение наблюдаемого источника.
    /// </summary>
    public abstract T Read();

    /// <summary>
    /// Сравнивает два значения. Переопределяется, если сравнение по умолчанию
    /// не подходит - например, для чисел с плавающей точкой.
    /// </summary>
    protected virtual bool AreEqual(T left, T right)
    {
        return EqualityComparer<T>.Default.Equals(left, right);
    }

    /// <summary>
    /// Забывает прочитанное значение: следующий запуск будет считаться первым.
    /// </summary>
    public void ResetValue()
    {
        Watcher.ResetValue();
    }

    /// <inheritdoc />
    bool IWatcherTask<T>.AreValuesEqual(T left, T right)
    {
        return AreEqual(left, right);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnExecute()
    {
        Watcher.Poll();
    }

    #endregion
}
