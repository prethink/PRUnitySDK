using System.Collections.Generic;
using YG;

/// <summary>
/// Реализация метрик для YGPlugin.
/// </summary>
public class YGMetrics : MetricBase
{
    #region Базовый класс

    /// <inheritdoc />
    public override void Send(string eventName)
    {
        YG2.MetricaSend(eventName);
    }

    /// <inheritdoc />
    public override void Send(string eventName, Dictionary<string, string> eventParams)
    {
        YG2.MetricaSend(eventName, eventParams);
    }

    /// <inheritdoc />
    public override void Send(string rootKeyEvent, string subKeyEvent, string subValueEvent)
    {
        Send(rootKeyEvent, new Dictionary<string, string> { { subKeyEvent, subValueEvent } });
    }

    #endregion
}
