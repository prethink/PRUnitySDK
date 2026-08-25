using System;

/// <summary>
/// Состояние временной награды на момент запроса.
/// </summary>
public readonly struct TimeLimitedRewardState
{
    /// <summary>
    /// Ключ награды.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Момент окончания действия.
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    /// Сколько осталось до окончания. Ноль, если награда уже истекла.
    /// </summary>
    public TimeSpan Remaining { get; }

    /// <summary>
    /// Действует ли награда сейчас.
    /// </summary>
    public bool IsActive => Remaining > TimeSpan.Zero;

    public TimeLimitedRewardState(string key, DateTime endTime, TimeSpan remaining)
    {
        Key = key;
        EndTime = endTime;
        Remaining = remaining;
    }
}
