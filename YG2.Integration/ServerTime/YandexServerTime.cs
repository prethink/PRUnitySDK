#if !PRSDK_DISABLE_YG2
using System;
using System.Diagnostics;
using YG;

/// <summary>
/// Время площадки Яндекс Игр: по нему считаются суточные подарки и таймеры.
/// </summary>
/// <remarks>
/// Площадка отдаёт время в UTC, поэтому «новый день» в релизе наступает в полночь по UTC,
/// а не по часам игрока.
/// <para>
/// Между обращениями к площадке время ведёт секундомер, а не часы устройства: иначе перевод
/// часов во время игры сдвигал бы и «серверное» время, и суточный подарок открывался бы сразу.
/// Раз в минуту время сверяется с площадкой заново — секундомер браузера может отстать,
/// пока устройство спит.
/// </para>
/// <para>
/// В отладочной сборке (<see cref="ReleaseType.Debug"/>) это просто часы устройства: так
/// суточные механики проверяют переводом времени.
/// </para>
/// </remarks>
public class YandexServerTime : IServerTime
{
    /// <summary>
    /// Как часто время сверяется с площадкой, секунды.
    /// </summary>
    private const double ResyncSeconds = 60d;

    /// <summary>
    /// Через сколько повторить сверку, пока площадка не ответила ни разу, секунды.
    /// </summary>
    /// <remarks>
    /// Короткий: до первой сверки время берётся с часов устройства, а их игрок переводит сам.
    /// </remarks>
    private const double RetrySeconds = 1d;

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
        // Местное время здесь сдвинуло бы дату на часовой пояс игрока.
        return synced ? syncedTime + sinceAttempt.Elapsed : DateTime.UtcNow;
    }

    /// <summary>
    /// Сверяет время с площадкой.
    /// </summary>
    public void Initialize()
    {
        if (PRUnitySDK.Settings.Project.ReleaseType == ReleaseType.Release && !synced)
            Sync();
    }

    private void Sync()
    {
        long milliseconds = 0;

        try
        {
            milliseconds = YG2.ServerTime();
        }
        catch (Exception ex)
        {
            PRLog.WriteWarning(this, ex);
        }

        // Ноль — площадка не готова или не ответила. Запоминать его нельзя: это 1 января 1970 года,
        // и всё, что записано с такой датой, на следующем запуске выглядело бы просроченным
        // на десятилетия — суточный подарок открылся бы заново.
        if (milliseconds <= 0)
        {
            // Уже сверенное время продолжает идти по секундомеру; следующая попытка — через минуту.
            if (synced)
                syncedTime += sinceAttempt.Elapsed;

            // Без первой сверки секундомер только откладывает повтор: иначе каждый запрос времени
            // обращался бы к площадке заново, а таймеры в интерфейсе спрашивают его каждый кадр.
            sinceAttempt.Restart();
            return;
        }

        syncedTime = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;
        sinceAttempt.Restart();
        synced = true;
    }
}
#endif
