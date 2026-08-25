using System;

/// <summary>
/// События временных наград.
/// </summary>
public static class TimeLimitedRewardEvents
{
    /// <summary>
    /// Публикует выдачу или продление награды.
    /// </summary>
    public static void RaiseChanged(string key, DateTime endTime, bool wasActive)
    {
        EventBus.RaiseEvent<ITimeLimitedRewardChangedEvent>(
            invoke => invoke.OnTimeLimitedRewardChanged(key, endTime, wasActive));
    }

    /// <summary>
    /// Публикует окончание действия награды.
    /// </summary>
    public static void RaiseExpired(string key, DateTime endTime)
    {
        EventBus.RaiseEvent<ITimeLimitedRewardExpiredEvent>(
            invoke => invoke.OnTimeLimitedRewardExpired(key, endTime));
    }
}
