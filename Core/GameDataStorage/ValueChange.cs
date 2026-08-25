/// <summary>
/// Результат установки значения в типизированном хранилище.
/// </summary>
/// <typeparam name="TValue">Тип значения.</typeparam>
public readonly struct ValueChange<TValue>
{
    /// <summary>
    /// Существовало ли значение до операции.
    /// </summary>
    public bool HadPreviousValue { get; }

    /// <summary>
    /// Значение до операции или default, если ключ отсутствовал.
    /// </summary>
    public TValue PreviousValue { get; }

    /// <summary>
    /// Значение после операции.
    /// </summary>
    public TValue CurrentValue { get; }

    /// <summary>
    /// Было ли хранилище фактически изменено.
    /// </summary>
    public bool Changed { get; }

    /// <summary>
    /// Создаёт описание изменения значения.
    /// </summary>
    public ValueChange(
        bool hadPreviousValue,
        TValue previousValue,
        TValue currentValue,
        bool changed)
    {
        HadPreviousValue = hadPreviousValue;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        Changed = changed;
    }
}
