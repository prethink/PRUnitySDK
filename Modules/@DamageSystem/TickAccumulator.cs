using UnityEngine;

/// <summary>
/// Счёт тиков периодического урона без сцены: сколько тиков наступило за прошедшее время.
/// </summary>
/// <remarks>
/// <para>
/// Длинный кадр (WebGL, вкладка в фоне) тиков не теряет: за шаг отдаётся до <see cref="MaxPerStep"/> тиков, остаток
/// сверх предела не копится — после долгой заморозки урон не приходит лавиной.
/// </para>
/// <para>
/// <see cref="Finish"/> — для эффекта, который кончается раньше, чем до тика дошёл счёт: статус снимается в том же кадре,
/// когда наступает последний тик, и без досчёта яд на 3 с с тиком 0,5 с давал бы пять ударов вместо шести.
/// </para>
/// </remarks>
public sealed class TickAccumulator
{
    // Запас на погрешность сложения float: 0,1 × 30 — это 2,9999…, а не 3.
    private const float Tolerance = 1e-4f;

    private readonly float interval;

    /// <summary>
    /// Создаёт счёт тиков.
    /// </summary>
    /// <param name="interval">Секунд между тиками.</param>
    /// <param name="maxPerStep">Сколько тиков отдавать за один шаг самое большее.</param>
    public TickAccumulator(float interval, int maxPerStep)
    {
        this.interval = Mathf.Max(0.01f, interval);
        MaxPerStep = Mathf.Max(1, maxPerStep);
    }

    /// <summary>
    /// Предел тиков за шаг.
    /// </summary>
    public int MaxPerStep { get; }

    /// <summary>
    /// Сколько прошло с последнего тика.
    /// </summary>
    public float Elapsed { get; private set; }

    /// <summary>
    /// Прибавляет время и отдаёт число наступивших тиков (не больше <see cref="MaxPerStep"/>).
    /// </summary>
    public int Add(float deltaTime)
    {
        Elapsed += Mathf.Max(0f, deltaTime);
        int due = 0;
        while (Elapsed + Tolerance >= interval && due < MaxPerStep)
        {
            Elapsed = Mathf.Max(0f, Elapsed - interval);
            due++;
        }

        // Предел догнали — остаток не копим, иначе следующие шаги отдали бы его пачкой.
        if (due == MaxPerStep)
            Elapsed = Mathf.Min(Elapsed, interval);

        return due;
    }

    /// <summary>
    /// Досчёт при окончании эффекта: прибавляет ещё не учтённое время этого кадра и отдаёт наступившие тики.
    /// Неполный интервал тиком не становится.
    /// </summary>
    /// <param name="uncountedDeltaTime">Время кадра, которое счёт ещё не получил; ноль — уже получил.</param>
    public int Finish(float uncountedDeltaTime)
    {
        int due = Add(uncountedDeltaTime);
        Elapsed = 0f;
        return due;
    }
}
