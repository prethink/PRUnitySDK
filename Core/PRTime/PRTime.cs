using System;
using UnityEngine;

public class PRTime : PRMonoBehaviourSingletonBase<PRTime>
{
    /// <summary>
    /// Общее время которое прошло с момента инициализации PRTime.
    /// </summary>
    public float Time { get; private set; }

    /// <summary>
    /// Текущее количество полных секунд, прошедших с момента инициализации PRTime.
    /// </summary>
    public long CurrentSecond { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, с учётом паузы логики.
    /// </summary>
    public float DeltaTime { get; private set; }

    /// <summary>
    /// Время прошедшее с последнего кадра, без учёта паузы логики.
    /// </summary>
    public float LastRawTime { get; private set; }

    /// <summary>
    /// Последнее значение количества полных секунд, прошедших с момента инициализации PRTime, при котором было вызвано событие OnNextSecond.
    /// </summary>
    private long lastSecond;

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
            this.LastRawTime = UnityEngine.Time.realtimeSinceStartup;
            this.DeltaTime = 0f;
        }
        base.Update();
        EventBus.RaiseEvent<IOnUpdateEvent>(x => x.OnUpdateEvent());
    }

    #endregion

    #region Базовый класс

    /// <inheritdoc />
    protected override void PRUpdate()
    {
        float rawTime = UnityEngine.Time.realtimeSinceStartup;

        float rawDelta = rawTime - LastRawTime;
        this.DeltaTime = rawDelta;
        this.Time += DeltaTime;

        this.LastRawTime = rawTime;

        CurrentSecond = Mathf.FloorToInt(this.Time);
        if (CurrentSecond != lastSecond)
        {
            lastSecond = CurrentSecond;
            EventBus.RaiseEvent<IOnSecondEvent>(x => x.OnSecondTick(CurrentSecond));
        }

        EventBus.RaiseEvent<IOnPRUpdateEvent>(x => x.OnPRUpdateEvent());
    }

    #endregion

    #region Методы

    /// <summary>
    /// Сбросить время.
    /// </summary>
    public void Reset()
    {
        this.Time = 0f;
        this.DeltaTime = 0f;
        this.LastRawTime = UnityEngine.Time.realtimeSinceStartup;
    }

    #endregion
}
