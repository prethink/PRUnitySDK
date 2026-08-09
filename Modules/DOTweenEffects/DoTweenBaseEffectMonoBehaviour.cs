using DG.Tweening;
using UnityEngine;

/// <summary>
/// Базовый компонент DOTween-эффекта с поддержкой логической паузы и слоёв PRTimeScale.
/// </summary>
public abstract class DoTweenBaseEffectMonoBehaviour : MonoBehaviour, IDoTweenEffect, ITimeScaleLayer, IOnPRTimeScaleChange
{
    #region Поля и свойства

    /// <summary>
    /// Созданная DOTween-анимация.
    /// </summary>
    protected Tween tween;

    /// <summary>
    /// Признак наличия созданной анимации.
    /// </summary>
    public bool IsCreated { get; protected set; } 

    #endregion

    #region MonoBehaviour

    [Header("Базовые настройки")]
    [SerializeField] protected Ease ease;
    [SerializeField] protected LoopType loopType;
    [SerializeField] protected int loopCount;
    [SerializeField, Min(0f)] protected float duration;
    [SerializeField] protected bool playAnimationOnStart;
    [SerializeField] protected bool ignorePauseNotify;

    /// <summary>
    /// Устанавливает функцию сглаживания.
    /// </summary>
    public DoTweenBaseEffectMonoBehaviour SetEase(Ease ease)
    {
        this.ease = ease;
        return this;
    }

    /// <summary>
    /// Устанавливает тип повторения.
    /// </summary>
    public DoTweenBaseEffectMonoBehaviour SetLoopType(LoopType loopType)
    {
        this.loopType = loopType;
        return this;
    }

    /// <summary>
    /// Устанавливает количество циклов. Значение -1 означает бесконечное повторение.
    /// </summary>
    public DoTweenBaseEffectMonoBehaviour SetLoopCount(int loopCount)
    {
        this.loopCount = loopCount;
        return this;
    }

    /// <summary>
    /// Устанавливает длительность одного цикла в секундах.
    /// </summary>
    public DoTweenBaseEffectMonoBehaviour SetDuration(float duration)
    {
        this.duration = Mathf.Max(0f, duration);
        return this;
    }

    /// <summary>
    /// Включает или отключает реакцию на логическую паузу SDK.
    /// </summary>
    public DoTweenBaseEffectMonoBehaviour SetIgnorePauseNotify(bool ignorePauseNotify)
    {
        this.ignorePauseNotify = ignorePauseNotify;
        return this;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    private void OnDestroy()
    {
        DestroyAnimation();
    }

    private void Start()
    {
        if (playAnimationOnStart)
            CreateAnimation();

        OnPauseStateChanged(new PauseStateEventArgs());
    }

    #endregion

    #region IPauseNotify

    /// <summary>
    /// Обрабатывает изменение логической паузы.
    /// </summary>
    public void OnPauseStateChanged(PauseStateEventArgs args)
    {
        if (ignorePauseNotify)
            return;

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            StopAnimation();
        else
            StartAnimation();
    }

    #endregion

    #region IDoTweenEffect

    public Ease Ease => ease;

    public LoopType LoopType => loopType;

    public int LoopCount => loopCount;

    public float Duration => duration;

    public bool PlayAnimationOnStart => playAnimationOnStart;

    /// <summary>
    /// Возобновляет созданную анимацию и применяет актуальный time scale.
    /// </summary>
    public virtual void StartAnimation()
    {
        tween?.Play();
        if(tween != null)
            tween.timeScale = PRTimeScale.Instance.Resolve(GetTimeScaleLayer());
    }

    /// <summary>
    /// Приостанавливает созданную анимацию с сохранением прогресса.
    /// </summary>
    public virtual void StopAnimation()
    {
        tween?.Pause();
    }

    /// <summary>
    /// Уничтожает анимацию и сбрасывает состояние компонента.
    /// </summary>
    public virtual void DestroyAnimation()
    {
        tween?.Kill();
        tween = null;
        IsCreated = false;
    }

    /// <summary>
    /// Пересоздаёт DOTween-анимацию согласно настройкам компонента.
    /// </summary>
    public abstract Tween CreateAnimation();

    /// <summary>
    /// Возвращает слой PRTimeScale, управляющий скоростью эффекта.
    /// </summary>
    public virtual Enumeration GetTimeScaleLayer()
    {
        return PRTimeScaleEnumerationProvider.Global;
    }

    /// <summary>
    /// Обновляет скорость tween при изменении его слоя PRTimeScale.
    /// </summary>
    public void OnPRTimeScaleChange(Enumeration layer, float value)
    {
        var effectLayer = GetTimeScaleLayer();
        if (tween != null && (layer == PRTimeScaleEnumerationProvider.Global || layer == effectLayer))
            tween.timeScale = PRTimeScale.Instance.Resolve(effectLayer);
    }


    #endregion
}
