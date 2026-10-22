#if !PRSDK_DISABLE_GAMEPUSH
using System;
using System.Diagnostics;
using GamePush;

/// <summary>
/// Время сервера GamePush: по нему считаются суточные подарки и таймеры.
/// </summary>
/// <remarks>
/// Устроено как <c>YandexServerTime</c>: время в UTC, между сверками его ведёт секундомер (часы устройства
/// игрок может перевести), раз в минуту оно сверяется с площадкой заново. В отладочной сборке
/// (<see cref="ReleaseType.Debug"/>) это просто часы устройства.
/// <para>
/// Плагин отдаёт время строкой и разбирает её сам; строка с «Z» превращается в местное время,
/// поэтому ответ приводится к UTC здесь. В редакторе плагин отдаёт часы устройства.
/// </para>
/// </remarks>
public class GamePushServerTime : IServerTime
{
    /// <summary>
    /// Как часто время сверяется с площадкой, секунды.
    /// </summary>
    private const double ResyncSeconds = 60d;

    /// <summary>
    /// Через сколько повторить сверку, пока площадка не ответила ни разу, секунды.
    /// </summary>
    private const double RetrySeconds = 1d;

    /// <summary>
    /// Ответ раньше этого года считается пустым значением площадки.
    /// </summary>
    private const int MinValidYear = 2020;

    /// <summary>
    /// Время с последней попытки сверки — удачной или нет.
    /// </summary>
    private readonly Stopwatch sinceAttempt = new();
    private DateTime syncedTime;
    private bool synced;

    public DateTime GetNow()
    {
        if (PRUnitySDK.Settings.Project.ReleaseType != ReleaseType.Release)
            return DateTime.Now;

        double interval = synced ? ResyncSeconds : RetrySeconds;

        if (!sinceAttempt.IsRunning || sinceAttempt.Elapsed.TotalSeconds >= interval)
            Sync();

        // Площадка ещё не ответила: часы устройства в той же шкале, что и её время, — в UTC.
        return synced ? syncedTime + sinceAttempt.Elapsed : DateTime.UtcNow;
    }

    private void Sync()
    {
        DateTime time = default;

        // Без поднятой площадки обращение к ней в браузере падает в JavaScript, и поймать это здесь нельзя.
        if (GP_Init.isReady)
        {
            try
            {
                time = GP_Server.Time();
            }
            catch (Exception exception)
            {
                PRLog.WriteWarning(this, exception);
            }
        }

        if (time.Year < MinValidYear)
        {
            // Уже сверенное время продолжает идти по секундомеру; следующая попытка — через интервал.
            if (synced)
                syncedTime += sinceAttempt.Elapsed;

            sinceAttempt.Restart();
            return;
        }

        // Kind сбрасывается, как у времени Яндекса: остальной код сравнивает даты без учёта зоны.
        DateTime utc = time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time;
        syncedTime = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        sinceAttempt.Restart();
        synced = true;
    }
}
#endif
