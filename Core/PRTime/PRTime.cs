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

    /// <inheritdoc />
    /// <remarks>
    /// Смена паузы начинает отсчёт кадра заново. Пауза по потере фокуса обычно совпадает
    /// с остановкой кадров: окно свернули, вкладку скрыли. <see cref="Update"/> в это время
    /// не идёт и отметку времени не двигает, поэтому первый кадр после возвращения считал бы
    /// шагом весь простой — и всё, что движется игровым временем, прыгало бы вперёд.
    /// <para>
    /// Отдельно слушать фокус и сворачивание приложения не нужно: <c>GameManager</c> переводит
    /// их в паузу SDK, и сюда они приходят этим же событием.
    /// </para>
    /// </remarks>
    public override void OnPauseStateChanged(PauseStateEventArgs args)
    {
        base.OnPauseStateChanged(args);

        // Следующий кадр отсчитывает время от этого момента, а не от прошлого кадра.
        LastRawTime = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        // Пока SDK загружается, время не идёт, но отметку двигаем: иначе первый кадр после загрузки посчитал бы
        // шагом всю загрузку, и секундные события выдали бы за неё доход и тики таймеров.
        if (!PRUnitySDK.IsInitialized)
        {
            this.LastRawTime = Time.realtimeSinceStartup;
            return;
        }

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
            GameFixedDeltaTime = RealFixedDeltaTime * PRTimeScale.Instance.GetWorldTimeScale();
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

        // Ускорение для проверки (PRDebugTime): кадр проживается как несколько — и игровое, и реальное время,
        // а часы таймеров уходят вперёд на ту же величину. В релизе множитель всегда 1.
        rawDelta = PRDebugTime.Advance(rawDelta);
        this.RealDeltaTime = rawDelta;
        this.RealTime += RealDeltaTime;
        this.LastRawTime = rawTime;

        CurrentRealSecond = Mathf.FloorToInt(this.RealTime);

        // По событию на каждую прошедшую секунду: обработчики считают «событие — секунда» (Timer, метрики), и при
        // подвисании кадра одно событие за несколько секунд их теряло.
        while (lastRealSecond < CurrentRealSecond)
        {
            long second = ++lastRealSecond;
            EventBus.RaiseEvent<IOnRealSecondsEvent>(x => x.OnRealSecondTick(second));
        }
    }

    private void UpdateGameTime()
    {
        // Игровое время идёт в темпе мира: замедление «всего, кроме игрока» его тоже замедляет.
        GameDeltaTime = this.RealDeltaTime * PRTimeScale.Instance.GetWorldTimeScale();
        GameTime += GameDeltaTime;

        CurrentGameSecond = Mathf.FloorToInt(this.GameTime);

        // По событию на каждую прошедшую секунду: доход StealEgg и Timer считают «событие — секунда», а за кадр
        // при подвисании или сильном ускорении проходит больше одной.
        while (lastGameSecond < CurrentGameSecond)
        {
            long second = ++lastGameSecond;
            EventBus.RaiseEvent<IOnGameSecondsEvent>(x => x.OnGameSecondTick(second));
        }
    }

    #endregion

    #region Методы

    /// <summary>
    /// Сбросить время.
    /// </summary>
    /// <remarks>
    /// Всё сразу — и реальное, и игровое время, и номера секунд: после частичного сброса номер текущей секунды
    /// оказался бы меньше последнего опубликованного, и секундные события замолчали бы до прежнего значения.
    /// Сроки модификаторов <see cref="PRTimeScale"/> отсчитываются по <see cref="RealTime"/>, поэтому они
    /// снимаются тоже.
    /// </remarks>
    public void Reset()
    {
        this.RealTime = 0f;
        this.GameTime = 0f;
        this.CurrentRealSecond = 0;
        this.CurrentGameSecond = 0;
        this.lastRealSecond = 0;
        this.lastGameSecond = 0;

        PRTimeScale.Instance.Reset();

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
