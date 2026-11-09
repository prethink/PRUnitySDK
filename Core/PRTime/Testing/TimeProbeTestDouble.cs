#if PRSDK_TESTS
using UnityEngine;

/// <summary>
/// Пробник времени для Play-теста «Время»: живёт по правилам SDK, как обычный компонент игры, и считает, сколько
/// времени до него дошло.
/// </summary>
/// <remarks>
/// Лежит вне <c>Editor</c>: компонент из Editor-сборки на объект не вешается. Поведение — как у компонентов игры:
/// шаг кадра — в <see cref="PRMonoBehaviour.PRUpdate"/> по своим слоям (<see cref="TimeLayerClock"/>), повтор раз в
/// <see cref="RepeatInterval"/> — <see cref="RepeatSecondsCoroutine"/>, запущенный в <c>OnEnable</c> и остановленный в
/// <c>OnDisable</c>. Тест сверяет накопленное с часами <see cref="PRTime"/>.
/// </remarks>
public class TimeProbeTestDouble : PRMonoBehaviour
{
    /// <summary>
    /// Слои времени пробника — как у персонажа: игрок, NPC, мир.
    /// </summary>
    public TimeLayer Layers = TimeLayer.World;

    /// <summary>
    /// Интервал повтора, секунды игрового времени.
    /// </summary>
    public float RepeatInterval = 0.25f;

    /// <summary>
    /// Шаг кадра повтора: по умолчанию — часы мира.
    /// </summary>
    public System.Func<float> RepeatClock;

    /// <summary>
    /// Сколько раз пришёл <see cref="PRMonoBehaviour.PRUpdate"/>.
    /// </summary>
    public int Updates { get; private set; }

    /// <summary>
    /// Сумма <see cref="PRTime.GameDeltaTime"/> за все <c>PRUpdate</c> — «путь» объекта мира.
    /// </summary>
    public float WorldTravel { get; private set; }

    /// <summary>
    /// Сумма шагов по своим слоям (<see cref="TimeLayerClock.DeltaTime"/>) — «путь» персонажа.
    /// </summary>
    public float LayerTravel { get; private set; }

    /// <summary>
    /// Сколько раз сработал повтор.
    /// </summary>
    public int RepeatTicks { get; private set; }

    /// <summary>
    /// <see cref="PRTime.GameTime"/> в кадре, когда повтор запустился (<c>OnEnable</c>): от него отсчитываются интервалы.
    /// </summary>
    public float RepeatStartGameTime { get; private set; }

    /// <summary>
    /// <see cref="PRTime.GameTime"/> последнего срабатывания повтора.
    /// </summary>
    public float LastRepeatGameTime { get; private set; }

    /// <summary>
    /// Сколько раз пришёл <see cref="PRMonoBehaviour.PREndOfFrame"/> — корутина кадра, которую запускает сам
    /// <see cref="PRMonoBehaviour"/> (как <c>PRLateFixedUpdate</c> у мотора игрока).
    /// </summary>
    public int EndOfFrames { get; private set; }

    /// <summary>
    /// Часы объекта по его слоям.
    /// </summary>
    public TimeLayerClock Clock { get; private set; }

    private RepeatSecondsCoroutine repeat;

    protected override void Awake()
    {
        base.Awake();
        Clock = new TimeLayerClock(() => Layers);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RepeatStartGameTime = PRTime.Instance.GameTime;
        RepeatTicks = 0;
        repeat = new RepeatSecondsCoroutine(OnRepeat, RepeatInterval, RepeatClock ?? RepeatSecondsCoroutine.GameClock,
            this, runOnStart: false);
        repeat.Execute();
    }

    protected override void OnDisable()
    {
        repeat?.Stop();
        base.OnDisable();
    }

    private void OnRepeat()
    {
        RepeatTicks++;
        LastRepeatGameTime = PRTime.Instance.GameTime;
    }

    protected override bool UseCoroutineWaitForEndOfFrame() => true;

    protected override void PREndOfFrame() => EndOfFrames++;

    protected override void PRUpdate()
    {
        Updates++;
        WorldTravel += PRTime.Instance.GameDeltaTime;
        LayerTravel += Clock.DeltaTime;
    }

    /// <summary>
    /// Обнулить счётчики перед замером.
    /// </summary>
    public void ResetCounters()
    {
        Updates = 0;
        EndOfFrames = 0;
        WorldTravel = 0f;
        LayerTravel = 0f;
    }
}
#endif
