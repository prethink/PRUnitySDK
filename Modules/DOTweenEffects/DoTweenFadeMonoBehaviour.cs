using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Меняет прозрачность объекта: затухание, проявление, мигание.
/// </summary>
/// <remarks>
/// Прозрачность — собственный слот, поэтому эффект спокойно соседствует с движением,
/// вращением и масштабом на одном объекте.
/// <para>
/// Цель ищется сама в том порядке, в каком её обычно и задают: <see cref="CanvasGroup"/>
/// гасит целое окно вместе с детьми, <see cref="Graphic"/> — одну картинку или подпись,
/// <see cref="SpriteRenderer"/> — спрайт в мире. Ссылку можно задать и руками, тогда
/// поиска не будет.
/// </para>
/// </remarks>
public class DoTweenFadeMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Fade")]
    [SerializeField, Range(0f, 1f), Tooltip("Прозрачность, к которой идёт эффект.")]
    private float targetAlpha;

    [SerializeField, Tooltip("Что гасить. Пусто - ищется на самом объекте.")]
    private Component target;

    /// <summary>
    /// Прозрачность, с которой эффект начинал.
    /// </summary>
    /// <remarks>
    /// Запоминается один раз и переживает пересоздание. <c>Yoyo</c> ходит между значением
    /// на момент старта и целью: анимация, пересозданная на полпути, мигала бы по уже
    /// укороченному размаху, и с каждым разом всё слабее.
    /// </remarks>
    private float baseAlpha;

    private bool baseCaptured;

    /// <summary>
    /// Задаёт целевую прозрачность для следующего <see cref="CreateAnimation"/>.
    /// </summary>
    public DoTweenFadeMonoBehaviour SetTargetAlpha(float targetAlpha)
    {
        this.targetAlpha = Mathf.Clamp01(targetAlpha);
        return this;
    }

    /// <summary>
    /// Задаёт, что гасить.
    /// </summary>
    /// <remarks>
    /// Принимает <see cref="CanvasGroup"/>, <see cref="Graphic"/> либо
    /// <see cref="SpriteRenderer"/>; остальное эффект гасить не умеет.
    /// </remarks>
    public DoTweenFadeMonoBehaviour SetTarget(Component target)
    {
        this.target = target;
        baseCaptured = false;

        return this;
    }

    /// <summary>
    /// Пересоздаёт анимацию прозрачности.
    /// </summary>
    /// <remarks>
    /// Без подходящей цели возвращает <c>null</c>, а <c>IsCreated</c> остаётся <c>false</c>:
    /// так же ведёт себя масштаб, которому нечего менять.
    /// </remarks>
    public override Tween CreateAnimation()
    {
        tween?.Kill();
        tween = null;
        IsCreated = false;

        ResolveTarget();

        if (!TryGetAlpha(out float current))
            return null;

        CaptureBaseAlpha(current);
        SetAlpha(baseAlpha);

        tween = CreateFade();

        if (tween == null)
            return null;

        tween = tween
            .SetLoops(loopCount, loopType)
            .SetEase(ease);

        IsCreated = true;

        return tween;
    }

    /// <summary>
    /// Строит твин под ту цель, которая нашлась.
    /// </summary>
    private Tween CreateFade()
    {
        return target switch
        {
            CanvasGroup canvasGroup => canvasGroup.DOFade(targetAlpha, duration),
            Graphic graphic => graphic.DOFade(targetAlpha, duration),
            SpriteRenderer spriteRenderer => spriteRenderer.DOFade(targetAlpha, duration),
            _ => null
        };
    }

    /// <summary>
    /// Ищет цель на самом объекте, если её не задали.
    /// </summary>
    private void ResolveTarget()
    {
        if (target != null)
            return;

        target = (Component)GetComponent<CanvasGroup>()
            ?? (Component)GetComponent<Graphic>()
            ?? GetComponent<SpriteRenderer>();
    }

    private bool TryGetAlpha(out float alpha)
    {
        switch (target)
        {
            case CanvasGroup canvasGroup:
                alpha = canvasGroup.alpha;
                return true;

            case Graphic graphic:
                alpha = graphic.color.a;
                return true;

            case SpriteRenderer spriteRenderer:
                alpha = spriteRenderer.color.a;
                return true;

            default:
                alpha = 1f;
                return false;
        }
    }

    private void SetAlpha(float alpha)
    {
        switch (target)
        {
            case CanvasGroup canvasGroup:
                canvasGroup.alpha = alpha;
                break;

            case Graphic graphic:
                graphic.color = WithAlpha(graphic.color, alpha);
                break;

            case SpriteRenderer spriteRenderer:
                spriteRenderer.color = WithAlpha(spriteRenderer.color, alpha);
                break;
        }
    }

    private void CaptureBaseAlpha(float current)
    {
        if (baseCaptured)
            return;

        baseAlpha = current;
        baseCaptured = true;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;

        return color;
    }
}
