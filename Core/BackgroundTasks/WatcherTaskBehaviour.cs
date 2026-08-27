using System.Collections.Generic;

/// <summary>
/// Задача-наблюдатель, живущая на объекте сцены.
/// Опрашивает значение и уведомляет подписчиков только в момент его изменения.
/// </summary>
/// <typeparam name="T">Тип наблюдаемого значения.</typeparam>
/// <remarks>
/// Отличается от <see cref="WatcherTask{T}"/> только тем, что является компонентом:
/// может ссылаться на объекты сцены, настраивается в инспекторе и регистрируется
/// при включении. Логика наблюдения у обоих одна и та же - <see cref="WatcherState{T}"/>.
/// </remarks>
public abstract class WatcherTaskBehaviour<T> : BackgroundTaskBehaviour, IWatcherTask<T>
{
    #region Поля и свойства

    private WatcherState<T> watcher;

    /// <summary>
    /// Наблюдаемое значение и его события.
    /// </summary>
    /// <remarks>
    /// Создаётся лениво: подписаться на изменения можно раньше, чем Unity вызовет Awake.
    /// </remarks>
    public WatcherState<T> Watcher => watcher ??= new WatcherState<T>(this);

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
    public virtual bool RaiseOnFirstRead => true;

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
