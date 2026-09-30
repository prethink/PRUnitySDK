#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Журнал метрик текущего запуска Editor, независимо от открытого окна Debug.
/// </summary>
public static class MetricDebugHistory
{
    public const int Capacity = 200;
    private static readonly object Sync = new();
    private static readonly Queue<MetricDebugEntry> Entries = new();
    private static long sequence;
    private static bool captureEnabled = true;

    /// <summary>
    /// Записывать новые отправки. Остановка сохраняет существующие записи.
    /// </summary>
    public static bool CaptureEnabled
    {
        get { lock (Sync) return captureEnabled; }
        set { lock (Sync) captureEnabled = value; }
    }

    /// <summary>
    /// Возвращает снимок от старых записей к новым.
    /// </summary>
    public static MetricDebugEntry[] Snapshot()
    {
        lock (Sync)
            return Entries.ToArray();
    }

    /// <summary>
    /// Очищает журнал, сохраняя настройку записи.
    /// </summary>
    public static void Clear()
    {
        lock (Sync)
            Entries.Clear();
    }

    internal static void Record(string eventName, IReadOnlyDictionary<string, object> parameters,
        string adapter, bool ignored)
    {
        if (!CaptureEnabled)
            return;

        DateTime timestamp = DateTime.UtcNow;
        string json;
        try
        {
            json = parameters == null || parameters.Count == 0
                ? "{}"
                : JsonConvert.SerializeObject(parameters, Formatting.Indented,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }
        catch (Exception exception)
        {
            // Диагностика не должна превращать успешную отправку в ошибку игрового кода.
            json = $"Parameters could not be captured: {exception.GetType().Name}: {exception.Message}";
        }

        lock (Sync)
        {
            if (!captureEnabled)
                return;

            while (Entries.Count >= Capacity)
                Entries.Dequeue();

            Entries.Enqueue(new MetricDebugEntry(++sequence, timestamp, eventName ?? string.Empty,
                adapter ?? string.Empty, json, ignored));
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        lock (Sync)
        {
            Entries.Clear();
            sequence = 0;
            captureEnabled = true;
        }
    }
}
#endif
