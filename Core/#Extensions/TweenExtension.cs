using DG.Tweening;
using System;
using UnityEngine;

public static class TweenExtension
{
    /// <summary>
    /// Зацикливает уменьшение до <paramref name="minScale"/> и возврат к исходному масштабу.
    /// </summary>
    /// <remarks>Владелец Tween должен остановить его через Kill, когда анимация больше не нужна.</remarks>
    public static Tween DoScaleUpDown(this GameObject gameObject, float minScale, float duration)
    {
        Vector3 originalScale = gameObject.transform.localScale;

        Sequence scaleSequence = DOTween.Sequence();

        scaleSequence.Append(gameObject.transform.DOScale(minScale, duration).SetEase(Ease.InOutQuad))
                     .Append(gameObject.transform.DOScale(originalScale, duration).SetEase(Ease.InOutQuad))
                     .SetLoops(-1, LoopType.Restart);

        return scaleSequence;
    }


    /// <summary>
    /// Плавно устанавливает единичный масштаб и вызывает callback после завершения.
    /// </summary>
    public static Tween ScaleToOneTween(this GameObject target, float duration, Action onComplete = null)
    {
        return target.transform
            .DOScale(Vector3.one, duration)
            .OnComplete(() => onComplete?.Invoke());
    }
}
