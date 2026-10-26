using System;
using UnityEngine;

/// <summary>
/// Ускорение времени для проверки: всё, что копится со временем, идёт в <see cref="Speed"/> раз быстрее.
/// </summary>
/// <remarks>
/// <para>
/// Так тестируют idle-игры: доход, тренировка, набор ресурсов, созревание, перезарядки — как если бы время шло быстрее.
/// Ускоряются шаг <see cref="PRTime"/> (игровое и реальное время, секундные события), часы таймеров
/// <see cref="PRUnitySDK.ServerTime"/> (к ним прибавляется <see cref="Offset"/>, накопленный ускорением) и весь игровой
/// процесс — движение, физика, анимации, боты (<c>Time.timeScale</c>). Звук и музыка идут как обычно.
/// </para>
/// <para>
/// Только в редакторе и отладочной сборке; в релизе множитель всегда 1. Каждый запуск начинается с обычного времени,
/// а сдвиг часов не сохраняется: таймеры, поставленные при ускорении, после перезапуска покажутся длиннее.
/// </para>
/// </remarks>
public static class PRDebugTime
{
    /// <summary>
    /// Самое большое ускорение.
    /// </summary>
    /// <remarks>
    /// Выше Unity не даёт: за кадр она продвигает игру не больше чем на Maximum Allowed Timestep (0,333 с), то есть
    /// на ~16 шагов физики, и при 60 FPS движение быстрее x20 уже не идёт, а <see cref="PRTime"/> шёл бы дальше.
    /// </remarks>
    public const float MaxSpeed = 20f;

    private static float speed = 1f;

    /// <summary>
    /// Ускорение можно включить: редактор или отладочная сборка.
    /// </summary>
    public static bool Allowed => Application.isEditor || Debug.isDebugBuild;

    /// <summary>
    /// Во сколько раз быстрее идёт время: 1 — обычно.
    /// </summary>
    public static float Speed
    {
        get => speed;
        set
        {
            speed = Allowed ? Mathf.Clamp(value, 1f, MaxSpeed) : 1f;
            // Движение, физика и анимации считают масштабированное время Unity; PRTime ускоряется отдельно —
            // он отсчитывает реальное время.
            Time.timeScale = speed;
        }
    }

    /// <summary>
    /// Насколько часы таймеров ушли вперёд от настоящих из-за ускорения.
    /// </summary>
    public static TimeSpan Offset { get; private set; }

    /// <summary>
    /// Сколько игра прожила за кадр в <paramref name="realDelta"/> настоящих секунд; часы таймеров уходят вперёд на разницу.
    /// </summary>
    /// <remarks>
    /// Не больше, чем за этот кадр прошла физика: Unity продвигает её не дальше <c>Time.maximumDeltaTime</c> за кадр
    /// (0,333 с). Без предела при низком FPS доход, воздух и таймеры уходили бы вперёд движения, а за кадр проходило бы
    /// несколько целых секунд — секундное событие же одно на кадр, и доход за остальные терялся.
    /// </remarks>
    public static float Advance(float realDelta)
    {
        if (speed <= 1f || realDelta <= 0f)
            return realDelta;

        float scaled = Mathf.Min(realDelta * speed, Mathf.Max(realDelta, Time.maximumDeltaTime));
        Offset += TimeSpan.FromSeconds(scaled - realDelta);
        return scaled;
    }

    /// <summary>
    /// Часы таймеров — снова настоящие.
    /// </summary>
    /// <remarks>
    /// Таймеры, поставленные при ускорении, хранят время окончания по ушедшим вперёд часам и после сброса
    /// затянутся на весь сдвиг.
    /// </remarks>
    public static void ResetClock() => Offset = TimeSpan.Zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        speed = 1f;
        Offset = TimeSpan.Zero;
        Time.timeScale = 1f;
    }
}
