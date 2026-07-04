using UnityEngine;

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

    /// Время прошедшее с последнего кадра, с учётом global layer time scale.
    /// </summary>
    public float GameDeltaTime { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, без учёта паузы логики.
    /// </summary>
    public float LastRawTime { get; private set; }

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

    /// <inheritdoc />
    protected override void Update()
    {
        if (!PRUnitySDK.IsInitialized)
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
        {
            this.LastRawTime = Time.realtimeSinceStartup;
            this.RealDeltaTime = 0f;
        }
        base.Update();
        EventBus.RaiseEvent<IOnUpdateEvent>(x => x.OnUpdateEvent());
    }

    #endregion

    #region Базовый класс

    /// <inheritdoc />
    protected override void PRUpdate()
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
        float globalScale = PRTimeScale.Instance.Resolve(PRTimeScaleEnumerationProvider.Global);
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
        this.LastRawTime = UnityEngine.Time.realtimeSinceStartup;
    }

    #endregion
}

public enum PRTimeType
{
    RealTime,
    GameTime
}