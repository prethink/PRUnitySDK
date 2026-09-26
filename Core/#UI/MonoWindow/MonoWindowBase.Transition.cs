using DG.Tweening;
using UnityEngine;

/// <summary>
/// Переход окна: появление и закрытие через DOTween.
/// </summary>
/// <remarks>
/// Состояние окна меняется сразу, а не по окончании перехода: закрываемое окно тут же
/// отпускает паузу и курсор и перестаёт быть видимым для трекера (<see cref="IsVisible"/>),
/// хотя ещё сжимается на экране. Иначе действие после закрытия — например, реклама — ждало
/// бы анимацию, а окно, открытое следом, закрыло бы уже закрытое.
/// <para>
/// Анимация идёт на unscaled time: окна открываются как раз на паузе.
/// </para>
/// </remarks>
public abstract partial class MonoWindowBase
{
    [Header("Переход")]
    [Tooltip("Default — переход из настроек проекта, Override — свой (Transition Override), None — без перехода.")]
    [SerializeField] protected MonoWindowTransitionMode transitionMode = MonoWindowTransitionMode.Default;

    [Tooltip("Свой переход окна: пресет или свои значения. Работает при Transition Mode = Override.")]
    [SerializeField] protected MonoWindowTransitionSettings transitionOverride = new();

    [Tooltip("Что вырастает и въезжает — обычно панель окна. Пусто — контейнер целиком, вместе с затемнением.")]
    [SerializeField] protected RectTransform transitionTarget;

    private Sequence transition;
    private bool isHiding;
    private Transform scaledTarget;
    private Vector3 restScale = Vector3.one;
    private Vector2 restPosition;
    private bool isSliding;
    private CanvasGroup fadeGroup;
    private float restAlpha = 1f;
    private bool restBlocksRaycasts = true;

    /// <summary>
    /// Переход окна или <see langword="null"/>, если окно появляется и закрывается сразу.
    /// </summary>
    /// <remarks>
    /// Окно переопределяет его, когда решение нужно принять в коде: всегда без перехода,
    /// свой переход для особого случая и т. п. Вызывается на каждое открытие и закрытие.
    /// <code>
    /// protected override MonoWindowTransition GetTransition() =>
    ///     MonoWindowTransition.FromPreset(MonoWindowTransitionPreset.SlideUp);
    /// </code>
    /// </remarks>
    protected virtual MonoWindowTransition GetTransition()
    {
        MonoWindowTransitionSettings settings = transitionMode switch
        {
            MonoWindowTransitionMode.Default => PRUnitySDK.Settings != null ? PRUnitySDK.Settings.WindowTransition : null,
            MonoWindowTransitionMode.Override => transitionOverride,
            _ => null,
        };

        return settings?.Resolve();
    }

    /// <summary>
    /// Запускает появление.
    /// </summary>
    /// <remarks>
    /// Прерванное закрытие не начинается заново: окно разворачивается оттуда, куда успело
    /// уйти, — повторное открытие не мигает.
    /// </remarks>
    private void PlayShowTransition()
    {
        bool interrupted = KillTransition();

        MonoWindowTransition settings = GetTransition();
        if (settings == null)
        {
            ResetTransitionState();
            return;
        }

        PrepareTransition(settings, captureRest: !interrupted);
        RectTransform slideTarget = GetSlideTarget(settings);

        if (!interrupted)
        {
            scaledTarget.localScale = restScale * settings.HiddenScale;

            if (slideTarget != null)
                slideTarget.anchoredPosition = GetHiddenPosition(slideTarget, settings);

            if (fadeGroup != null)
                fadeGroup.alpha = 0f;
        }

        transition = DOTween.Sequence()
            .Join(scaledTarget.DOScale(restScale, settings.ShowDuration).SetEase(settings.ShowEase));

        if (slideTarget != null)
            transition.Join(slideTarget.DOAnchorPos(restPosition, settings.ShowDuration).SetEase(settings.ShowEase));

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = restBlocksRaycasts;
            transition.Join(fadeGroup.DOFade(restAlpha, settings.ShowDuration).SetEase(Ease.OutQuad));
        }

        transition
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => transition = null);
    }

    /// <summary>
    /// Запускает закрытие; контейнер выключится в его конце.
    /// </summary>
    /// <returns><see langword="false"/>, если перехода нет и контейнер надо выключить сразу.</returns>
    private bool TryPlayHideTransition()
    {
        bool interrupted = KillTransition();

        MonoWindowTransition settings = GetTransition();

        // Невидимое окно анимировать незачем: анимацию никто не увидит, а контейнер провисит лишнее.
        if (settings == null || !GetContainer().activeInHierarchy)
            return false;

        PrepareTransition(settings, captureRest: !interrupted);
        RectTransform slideTarget = GetSlideTarget(settings);
        isHiding = true;

        transition = DOTween.Sequence()
            .Join(scaledTarget.DOScale(restScale * settings.HiddenScale, settings.HideDuration).SetEase(settings.HideEase));

        if (slideTarget != null)
        {
            transition.Join(slideTarget.DOAnchorPos(GetHiddenPosition(slideTarget, settings), settings.HideDuration)
                .SetEase(settings.HideEase));
        }

        if (fadeGroup != null)
        {
            // Нажатие по уходящему окну ответило бы на вопрос, который уже закрыт.
            fadeGroup.blocksRaycasts = false;
            transition.Join(fadeGroup.DOFade(0f, settings.HideDuration).SetEase(Ease.InQuad));
        }

        transition
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(FinishHideTransition);

        return true;
    }

    /// <summary>
    /// Цель, которую переход двигает, или <see langword="null"/>, если смещения нет.
    /// </summary>
    /// <remarks>
    /// Без смещения место цели не трогается вовсе: иначе переход спорил бы с разметкой,
    /// которая ставит панель сама, и возвращал бы её туда, где она стояла раньше.
    /// </remarks>
    private RectTransform GetSlideTarget(MonoWindowTransition settings)
    {
        bool slides = settings.SlideOffset != Vector2.zero && scaledTarget is RectTransform;
        isSliding |= slides;

        return slides ? (RectTransform)scaledTarget : null;
    }

    /// <summary>
    /// Где цель стоит, пока окно скрыто: смещение задано в долях её размера.
    /// </summary>
    /// <remarks>
    /// В долях, а не в пикселях: одно и то же «снизу на треть» подходит и маленькому
    /// вопросу, и окну во весь экран.
    /// </remarks>
    private Vector2 GetHiddenPosition(RectTransform target, MonoWindowTransition settings)
    {
        return restPosition + Vector2.Scale(settings.SlideOffset, target.rect.size);
    }

    private void FinishHideTransition()
    {
        transition = null;
        isHiding = false;
        GetContainer().SetActive(false);
        ResetTransitionState();
    }

    /// <summary>
    /// Обрывает переход, оставляя окно в том виде, в каком его застали.
    /// </summary>
    /// <returns><see langword="true"/>, если переход шёл.</returns>
    private bool KillTransition()
    {
        bool wasRunning = transition != null;

        transition?.Kill();
        transition = null;
        isHiding = false;

        return wasRunning;
    }

    /// <summary>
    /// Обрывает переход и возвращает окну обычный вид.
    /// </summary>
    private void StopTransition()
    {
        KillTransition();
        ResetTransitionState();
    }

    /// <summary>
    /// Находит, что двигать и что гасить.
    /// </summary>
    /// <remarks>
    /// Обычные масштаб и место цели запоминаются перед каждым переходом, который застал
    /// окно в покое: к ним окно и возвращается, а не к единице и нулю, — у панели в префабе
    /// они бывают любыми, а окно может и само подвинуть её между показами.
    /// </remarks>
    /// <param name="captureRest">
    /// Цель в покое. <see langword="false"/> — переход прерван посреди пути, и её нынешний
    /// вид обычным считать нельзя.
    /// </param>
    private void PrepareTransition(MonoWindowTransition settings, bool captureRest)
    {
        Transform target = transitionTarget != null ? transitionTarget : GetContainer().transform;

        if (target != scaledTarget)
        {
            ResetTransitionState();
            scaledTarget = target;
            captureRest = true;
        }

        if (captureRest)
        {
            restScale = target.localScale;
            restPosition = target is RectTransform rect ? rect.anchoredPosition : Vector2.zero;
        }

        if (settings.Fade)
        {
            if (fadeGroup == null)
            {
                fadeGroup = GetFadeGroup();
                captureRest = true;
            }

            // Своё состояние группы окно могло задать само: переход возвращает к нему,
            // а не к «видно и нажимается».
            if (captureRest)
            {
                restAlpha = fadeGroup.alpha;
                restBlocksRaycasts = fadeGroup.blocksRaycasts;
            }

            return;
        }

        if (fadeGroup == null)
            return;

        fadeGroup.alpha = restAlpha;
        fadeGroup.blocksRaycasts = restBlocksRaycasts;
        fadeGroup = null;
    }

    /// <summary>
    /// <see cref="CanvasGroup"/> контейнера; нет своего — добавляется.
    /// </summary>
    /// <remarks>
    /// Гасится контейнер, а не цель перехода: вместе с панелью должно проявиться и затемнение.
    /// </remarks>
    private CanvasGroup GetFadeGroup()
    {
        GameObject windowContainer = GetContainer();

        return windowContainer.TryGetComponent(out CanvasGroup group)
            ? group
            : windowContainer.AddComponent<CanvasGroup>();
    }

    private void ResetTransitionState()
    {
        if (scaledTarget != null)
        {
            scaledTarget.localScale = restScale;

            if (isSliding && scaledTarget is RectTransform rect)
                rect.anchoredPosition = restPosition;
        }

        isSliding = false;

        if (fadeGroup != null)
        {
            fadeGroup.alpha = restAlpha;
            fadeGroup.blocksRaycasts = restBlocksRaycasts;
        }
    }
}
