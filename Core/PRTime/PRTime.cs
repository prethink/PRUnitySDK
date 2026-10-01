using UnityEngine;

/// <summary>
/// Игровое и реальное время SDK с учётом паузы и масштаба времени.
/// </summary>
/// <remarks>
/// Работает и на паузе - обнуляет дельты, - поэтому у него свои методы кадра, а не PRUpdate
/// через раннер. Порядок -1000 ставит его раньше <see cref="PRMonoBehaviourHost"/>: все,
/// кого раннер вызывает в этом кадре, читают уже посчитанное время.
/// </remarks>
[DefaultExecutionOrder(-1000)]
public class PRTime : PRMonoBehaviourSingletonBase<PRTime>
{
    /// <summary>
    /// Общее время которое прошло с момента инициализации PRTime.
    /// </summary>
    public float RealTime { get; private set; }

    /// <summary>
    /// Игровое время, которое прошло с момента инициализации PRTime, с учётом global layer time scale.
    /// </summary>
    public float GameTime { get; private set; }

    /// <summary>
    /// Текущее количество полных секунд, прошедших с момента инициализации PRTime.
    /// </summary>
    public long CurrentRealSecond { get; private set; }

    /// <summary>
    /// Текущее количество полных секунд, прошедших с момента инициализации PRTime, с учётом global layer time scale.
    /// </summary>
    public long CurrentGameSecond { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, с учётом паузы логики.
    /// </summary>
    public float RealDeltaTime { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, с учётом global layer time scale.
    /// </summary>
    public float GameDeltaTime { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, без учёта паузы логики.
    /// </summary>
    public float LastRawTime { get; private set; }

    /// <summary>
    /// Фактический фиксированный шаг времени физики Unity (Time.fixedDeltaTime),
    /// не зависит от timeScale и используется как базовый физический timestep.
    /// </summary>
    public float RealFixedDeltaTime { get; private set; }

    /// <summary>
    /// Фиксированный шаг времени физики с учётом глобального time scale.
    /// Используется для игровой логики/кастомной симуляции (например Physics.Simulate),
    /// отражает “игровое” течение физического времени.
    /// </summary>
    public float GameFixedDeltaTime { get; private set; }

    /// <summary>
    /// Последнее значение количества полных секунд, прошедших с момента инициализации PRTime, при котором было вызвано событие OnNextSecond.
    /// </summary>
    private long lastRealSecond;

    /// <summary>
    /// Последнее значение количества полных секунд, прошедших с момента инициализации PRTime, при котором было вызвано событие OnNextSecond, с учётом global layer time scale.
    /// </summary>
    private long lastGameSecond;

    #region MonoBehaviour

    /// <inheritdoc />
    protected override void Awake()
    {
        base.Awake();

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Reset();
    }

    private void Update()
    {
        if (!PRUnitySDK.IsInitialized)
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
        {
            this.LastRawTime = Time.realtimeSinceStartup;
            this.RealDeltaTime = 0f;
            this.GameDeltaTime = 0f;
        }
        else
        {
            AdvanceTime();
        }

        EventBus.RaiseEvent<IOnUpdateEvent>(x => x.OnUpdateEvent());
    }

    private void FixedUpdate()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
        {
            this.LastRawTime = Time.realtimeSinceStartup;
            this.RealFixedDeltaTime = 0f;
            this.GameFixedDeltaTime = 0f;
            //TODO: OnFixedUpdateEvent
        }
        else
        {
            RealFixedDeltaTime = Time.fixedDeltaTime;
            GameFixedDeltaTime = RealFixedDeltaTime * PRTimeScale.Instance.GetGlobalTimeScale();
            //TODO: PROnFixedUpdateEvent
        }
    }

    #endregion

    #region Методы времени

    /// <summary>
    /// Шаг времени вне паузы. Прежде это был PRUpdate, но тогда раннер вызывал бы его в
    /// случайном месте кадра, а время должно быть посчитано до всех.
    /// </summary>
    private void AdvanceTime()
    {
        UpdateRealTime();
        UpdateGameTime();
        EventBus.RaiseEvent<IOnPRUpdateEvent>(x => x.OnPRUpdateEvent());
    }

    private void UpdateRealTime()
    {
        float rawTime = Time.realtimeSinceStartup;
        float rawDelta = rawTime - LastRawTime;
        this.RealDeltaTime = rawDelta;
        this.RealTime += RealDeltaTime;
        this.LastRawTime = rawTime;

        CurrentRealSecond = Mathf.FloorToInt(this.RealTime);
        if (CurrentRealSecond != lastRealSecond)
        {
            lastRealSecond = CurrentRealSecond;
            EventBus.RaiseEvent<IOnRealSecondsEvent>(x => x.OnRealSecondTick(CurrentRealSecond));
        }
    }

    private void UpdateGameTime()
    {
        float globalScale = PRTimeScale.Instance.Resolve();
        GameDeltaTime = this.RealDeltaTime * globalScale;
        GameTime += GameDeltaTime;

        CurrentGameSecond = Mathf.FloorToInt(this.GameTime);
        if (CurrentGameSecond != lastGameSecond)
        {
            lastGameSecond = CurrentGameSecond;
            EventBus.RaiseEvent<IOnGameSecondsEvent>(x => x.OnGameSecondTick(CurrentGameSecond));
        }
    }

    #endregion

    #region Методы

    /// <summary>
    /// Сбросить время.
    /// </summary>
    public void Reset()
    {
        this.RealTime = 0f;

        this.RealDeltaTime = 0f;
        this.GameDeltaTime = 0f;

        this.RealFixedDeltaTime = 0f;
        this.GameFixedDeltaTime = 0f;

        this.LastRawTime = Time.realtimeSinceStartup;
    }

    #endregion
}

public enum PRTimeType
{
    RealTime,
    GameTime
}
