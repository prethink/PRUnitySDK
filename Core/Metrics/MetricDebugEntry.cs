#if UNITY_EDITOR
using System;

/// <summary>
/// Неизменяемый снимок вызова адаптера метрик.
/// </summary>
public sealed class MetricDebugEntry
{
    /// <summary>
    /// Номер записи в текущем журнале.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Время передачи события адаптеру.
    /// </summary>
    public DateTime TimestampUtc { get; }

    /// <summary>
    /// Имя события, переданное адаптеру.
    /// </summary>
    public string EventName { get; }
    /// <summary>
    /// Тип адаптера, обработавшего вызов.
    /// </summary>
    public string Adapter { get; }
    /// <summary>
    /// Снимок переданных параметров после нормализации адаптером.
    /// </summary>
    public string ParametersJson { get; }

    /// <summary>
    /// Адаптер намеренно пропустил отправку, например DummyMetric.
    /// </summary>
    public bool Ignored { get; }

    internal MetricDebugEntry(long sequence, DateTime timestampUtc, string eventName,
        string adapter, string parametersJson, bool ignored)
    {
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        EventName = eventName;
        Adapter = adapter;
        ParametersJson = parametersJson;
        Ignored = ignored;
    }
}
#endif
