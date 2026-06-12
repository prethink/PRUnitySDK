using DG.Tweening;
using System;
using UnityEngine;

public static class TweenExtension
{
    /// <summary>
    /// Анимация уменьшения и увеличения объекта.
    /// </summary>
    /// <param name="gameObject">Целевой объект, которому будет применена анимация.</param>
    /// <param name="minScale">Минимальный масштаб, до которого объект будет уменьшен.</param>
    /// <param name="duration">Продолжительность анимации для уменьшения или увеличения.</param>
    /// <returns>Возвращает Tween для дальнейшего контроля анимации.</returns>
    public static Tween DoScaleUpDown(this GameObject gameObject, float minScale, float duration)
    {
        // Сохраняем текущий масштаб объекта
        Vector3 originalScale = gameObject.transform.localScale;

        // Создаем последовательность анимаций
        Sequence scaleSequence = DOTween.Sequence();

        // Добавляем анимации уменьшения и увеличения в последовательность
        scaleSequence.Append(gameObject.transform.DOScale(minScale, duration).SetEase(Ease.InOutQuad))
                     .Append(gameObject.transform.DOScale(originalScale, duration).SetEase(Ease.InOutQuad))
                     .SetLoops(-1, LoopType.Yoyo);

        return scaleSequence;
    }


    /// <summary>
    /// Плавно изменяет масштаб объекта до Vector3.one и вызывает callback после завершения.
    /// </summary>
    /// <param name="target">Transform объекта, который будет анимироваться.</param>
    /// <param name="duration">Длительность анимации.</param>
    /// <param name="onComplete">Callback, вызываемый после завершения анимации.</param>
    /// <returns>Tween, который можно дополнительно контролировать (kill, pause, etc.).</returns>
    public static Tween ScaleToOneTween(this GameObject target, float duration, Action onComplete = null)
    {
        return target.transform
            .DOScale(Vector3.one, duration)
            .OnComplete(() => onComplete?.Invoke());
    }
}
