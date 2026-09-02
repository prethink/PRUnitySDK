using UnityEngine;

/// <summary>
/// Точки вызова событий ботов.
/// </summary>
/// <remarks>
/// Класс partial: маршруты и другие проектные способы задать поведение бота
/// добавляются отдельными частями рядом со своим модулем.
/// </remarks>
public partial class BotEvents
{
    /// <summary>
    ///
    /// </summary>
    public static void Stop() => EventBus.RaiseEvent<IBotStopEvent>(x => x.StopEvent(new BotStopEventArgs()));
    public static void Stop(long botId) => EventBus.RaiseEvent<IBotStopEvent>(x => x.StopEvent(new BotStopEventArgs(botId)));

    public static void Start() => EventBus.RaiseEvent<IBotStartEvent>(x => x.StartEvent(new BotStartEventArgs()));
    public static void Start(long botId) => EventBus.RaiseEvent<IBotStartEvent>(x => x.StartEvent(new BotStartEventArgs(botId)));

    public static void SetTarget(Transform target) => EventBus.RaiseEvent<IBotSetTargetEvent>(x => x.SetPathEvent(new BotSetTargetEventArgs(target)));
    public static void SetTarget(Transform target, long botId) => EventBus.RaiseEvent<IBotSetTargetEvent>(x => x.SetPathEvent(new BotSetTargetEventArgs(target, botId)));
}
