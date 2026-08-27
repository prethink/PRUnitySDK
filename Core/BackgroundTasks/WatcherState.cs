using System;

/// <summary>
/// Наблюдаемое значение задачи: хранит последнее прочитанное, сравнивает с новым
/// и уведомляет подписчиков только при изменении.
/// </summary>
/// <typeparam name="T">Тип наблюдаемого значения.</typeparam>
/// <remarks>
/// Реализующая сторона моста для <see cref="IWatcherTask{T}"/>: владелец отвечает на
/// вопрос «как прочитать значение», этот класс - «что считать изменением и кого
/// об этом известить».
/// </remarks>
public sealed class WatcherState<T>
{
    #region Поля и свойства

    private readonly IWatcherTask<T> owner;

    /// <summary>
    /// Задача, которой принадлежит это значение.
    /// </summary>
    public IWatcherTask<T> Owner => owner;

    /// <summary>
    /// Последнее прочитанное значение.
    /// </summary>
    public T CurrentValue { get; private set; }

    /// <summary>
    /// Значение уже было прочитано хотя бы один раз.
    /// </summary>
    public bool HasValue { get; private set; }

    #endregion

    #region События

    /// <summary>
    /// Значение изменилось. Передаётся новое значение.
    /// </summary>
    public event Action<T> Changed;

    /// <summary>
    /// Значение изменилось. Передаются предыдущее и новое значения.
    /// </summary>
    public event Action<T, T> ChangedWithPrevious;

    #endregion

    #region Конструктор

    /// <summary>
    /// Создаёт наблюдаемое значение для указанной задачи.
    /// </summary>
    /// <param name="owner">Задача-владелец.</param>
    public WatcherState(IWatcherTask<T> owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    #endregion

    #region Методы

    /// <summary>
    /// Читает значение у владельца и поднимает события, если оно изменилось.
    /// </summary>
    /// <remarks>
    /// Исключение из <c>Read()</c> наружу не перехватывается: его обработает
    /// <see cref="BackgroundTaskRuntime"/>, который считает ошибки и при череде сбоев
    /// отключает задачу.
    /// </remarks>
    public void Poll()
    {
        T value = owner.Read();

        if (!HasValue)
        {
            CurrentValue = value;
            HasValue = true;

            if (owner.RaiseOnFirstRead)
                RaiseChanged(default, value);

            return;
        }

        if (owner.AreValuesEqual(CurrentValue, value))
            return;

        T previous = CurrentValue;
        CurrentValue = value;
        RaiseChanged(previous, value);
    }

    /// <summary>
    /// Забывает прочитанное значение: следующий опрос будет считаться первым.
    /// </summary>
    public void ResetValue()
    {
        CurrentValue = default;
        HasValue = false;
    }

    private void RaiseChanged(T previous, T current)
    {
        Changed?.Invoke(current);
        ChangedWithPrevious?.Invoke(previous, current);
    }

    #endregion
}
