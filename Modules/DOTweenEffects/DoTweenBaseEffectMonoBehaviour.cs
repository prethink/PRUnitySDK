using DG.Tweening;
using UnityEngine;

public abstract class DoTweenBaseEffectMonoBehaviour : MonoBehaviour, IDoTweenEffect
{
    #region Поля и свойства

    protected Tween tween;

    public bool IsCreated { get; protected set; } 

    #endregion

    #region MonoBehaviour

    [Header("Базовые настройки")]
    [SerializeField] protected Ease ease;
    [SerializeField] protected LoopType loopType;
    [SerializeField] protected int loopCount;
    [SerializeField] protected float duration;
    [SerializeField] protected bool playAnimationOnStart;
    [SerializeField] protected bool ignorePauseNotify;

    public DoTweenBaseEffectMonoBehaviour SetEase(Ease ease)
    {
        this.ease = ease;
        return this;
    }

    public DoTweenBaseEffectMonoBehaviour SetLoopType(LoopType loopType)
    {
        this.loopType = loopType;
        return this;
    }

    public DoTweenBaseEffectMonoBehaviour SetLoopCount(int loopCount)
    {
        this.loopCount = loopCount;
        return this;
    }

    public DoTweenBaseEffectMonoBehaviour SetDuration(float duration)
    {
        this.duration = duration;
        return this;
    }

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

    public virtual void StartAnimation()
    {
        tween?.Play();
    }

    public virtual void StopAnimation()
    {
        tween?.Pause();
    }

    public virtual void DestroyAnimation()
    {
        tween?.Kill();
    }

    public abstract Tween CreateAnimation();


    #endregion
}
