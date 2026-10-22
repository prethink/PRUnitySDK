#if !PRSDK_DISABLE_GAMEPUSH
using System;
using System.Collections.Generic;
using GamePush;
using Newtonsoft.Json;

/// <summary>
/// Цели аналитики GamePush (<c>GP_Analytics.Goal</c>): площадка пересылает их в подключённые у неё счётчики.
/// </summary>
/// <remarks>
/// У цели GamePush одно значение-строка, поэтому параметры события уходят в него одной строкой JSON.
/// </remarks>
public class GamePushMetrics : MetricBase
{
    /// <summary>
    /// Приставка идентификатора цели: событие <c>level_up</c> уходит как <c>_level_up</c>.
    /// </summary>
    /// <remarks>
    /// Та же, что у <c>YGMetrics</c>: цели в счётчиках заводят условием «идентификатор содержит»,
    /// и одинаковые имена на обеих площадках позволяют переносить настройки счётчика как есть.
    /// </remarks>
    public const string GoalPrefix = "_";

    /// <inheritdoc />
    public override void Send(string eventName)
    {
        eventName = GetGoalId(eventName);

        if (GP_Init.isReady)
            GP_Analytics.Goal(eventName, string.Empty);

        ReportSend(eventName, null, !GP_Init.isReady);
    }

    /// <inheritdoc />
    public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams)
    {
        eventName = GetGoalId(eventName);

        string value = eventParams == null || eventParams.Count == 0
            ? string.Empty
            : SerializeParams(eventParams);

        if (GP_Init.isReady)
            GP_Analytics.Goal(eventName, value);

        ReportSend(eventName, eventParams, !GP_Init.isReady);
    }

    private static string GetGoalId(string eventName)
    {
        return string.IsNullOrEmpty(eventName) || eventName.StartsWith(GoalPrefix, StringComparison.Ordinal)
            ? eventName
            : GoalPrefix + eventName;
    }

    /// <remarks>
    /// Значение, которое не сериализуется, не должно стоить цели: она уходит без параметров.
    /// </remarks>
    private string SerializeParams(IReadOnlyDictionary<string, object> eventParams)
    {
        try
        {
            return JsonConvert.SerializeObject(eventParams, Formatting.None);
        }
        catch (Exception exception)
        {
            PRLog.WriteWarning(this, exception);
            return string.Empty;
        }
    }
}
#endif
