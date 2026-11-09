using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Повтор раз в N секунд по выбранным часам — вместо счётчика «пора ли» в <c>Update</c>.
/// </summary>
/// <remarks>
/// Часы задаёт шаг кадра: <see cref="PRTime.GameDeltaTime"/> для мира, <see cref="PRTime.GetDeltaTime"/> по слоям для
/// персонажа и интерфейса в мире, <see cref="PRTime.UiDeltaTime"/> для окон и обучения, которые идут и на логической
/// паузе. Пауза отдельно не ждётся: на паузе шаг этих часов нулевой, и отсчёт стоит сам.
/// <para>
/// Корутина на владельце останавливается вместе с ним, а Unity обрывает её и при выключении объекта, поэтому запускать
/// её — в <c>OnEnable</c> (или по событию), останавливать — в <c>OnDisable</c>.
/// </para>
/// </remarks>
public class RepeatSecondsCoroutine : PRCoroutineBase
{
    private readonly Action callback;
    private readonly Func<float> deltaTime;
    private readonly bool runOnStart;
    private float interval;

    /// <summary>
    /// Шаг кадра игровых часов (темп мира, стоит на паузе).
    /// </summary>
    public static readonly Func<float> GameClock = () => PRTime.Instance.GameDeltaTime;

    /// <summary>
    /// Шаг кадра интерфейса в мире (слой <see cref="TimeLayer.UI"/>, стоит на логической паузе вместе с миром).
    /// </summary>
    public static readonly Func<float> UiLayerClock = () => PRTime.Instance.GetDeltaTime(TimeLayer.UI);

    /// <summary>
    /// Шаг кадра окон и обучения: идёт на логической паузе, стоит на паузе проекта.
    /// </summary>
    public static readonly Func<float> UiClock = () => PRTime.Instance.UiDeltaTime;

    /// <summary>
    /// Создаёт повтор.
    /// </summary>
    /// <param name="callback">Что делать раз в <paramref name="interval"/> секунд.</param>
    /// <param name="interval">Секунды между вызовами.</param>
    /// <param name="deltaTime">Шаг кадра часов: <see cref="GameClock"/>, <see cref="UiClock"/>, <see cref="UiLayerClock"/>
    /// или свой (по слоям сущности).</param>
    /// <param name="instance">Владелец корутины; <c>null</c> — глобальный хост.</param>
    /// <param name="runOnStart">Вызвать сразу при запуске, а не через интервал.</param>
    public RepeatSecondsCoroutine(Action callback, float interval, Func<float> deltaTime, MonoBehaviour instance,
        bool runOnStart = true)
        : base(instance)
    {
        this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
        this.deltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
        this.interval = interval;
        this.runOnStart = runOnStart;
    }

    /// <summary>
    /// Меняет интервал; действует со следующего отсчёта.
    /// </summary>
    public void SetInterval(float seconds) => interval = seconds;

    /// <inheritdoc />
    /// <remarks>
    /// За длинный кадр вызов приходит один раз, а не пачкой: это опрос состояния, догонять пропущенные незачем. Перелёт
    /// за интервал переносится в следующий: обнуление теряло его каждый раз, и при ускорении ×5 повтор отставал от игры
    /// на треть кадра за интервал (Play-тест «Время», «Ускорение 5×»). Целые пропущенные интервалы отбрасываются.
    /// </remarks>
    protected override IEnumerator InternalExecute()
    {
        if (runOnStart)
            callback();

        float elapsed = 0f;
        while (true)
        {
            yield return null;

            elapsed += deltaTime();
            float step = Mathf.Max(0.0001f, interval);
            if (elapsed < step)
                continue;

            elapsed %= step;
            callback();
        }
    }
}
