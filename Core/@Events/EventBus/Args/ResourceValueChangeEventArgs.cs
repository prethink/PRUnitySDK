/// <summary>
/// Параметры изменения числового значения игрового ресурса.
/// </summary>
public class ResourceValueChangeEventArgs : ResourceEventArgs
{
    /// <summary>
    /// Предыдущее значение ресурса. Осмысленно только при
    /// <see cref="HasPreviousValue"/> = true.
    /// </summary>
    public long PreviousValue { get; protected set; }

    /// <summary>
    /// Текущее значение ресурса.
    /// </summary>
    public long CurrentValue => Value;

    /// <summary>
    /// Известно ли предыдущее значение. False, когда событие опубликовано без него -
    /// например, при первом появлении ресурса.
    /// </summary>
    public bool HasPreviousValue { get; protected set; }

    /// <summary>
    /// Разница между текущим и предыдущим значением либо null, если предыдущее
    /// значение неизвестно. Отдельно от нуля: ноль означает "значение не изменилось",
    /// а null - "сравнивать не с чем".
    /// </summary>
    public long? Delta => HasPreviousValue ? CurrentValue - PreviousValue : null;

    /// <summary>
    /// Совместимый alias текущего значения.
    /// </summary>
    public long Value { get; protected set; }

    /// <summary>
    /// Создаёт событие без известного предыдущего значения.
    /// </summary>
    public ResourceValueChangeEventArgs(Enumeration resourceType, long value)
        : base(resourceType)
    {
        Value = value;
        PreviousValue = default;
        HasPreviousValue = false;
    }

    /// <summary>
    /// Создаёт событие с предыдущим и текущим значениями.
    /// </summary>
    public ResourceValueChangeEventArgs(
        Enumeration resourceType,
        long previousValue,
        long currentValue)
        : base(resourceType)
    {
        PreviousValue = previousValue;
        Value = currentValue;
        HasPreviousValue = true;
    }
}
