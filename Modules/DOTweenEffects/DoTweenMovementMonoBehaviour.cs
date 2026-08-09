using DG.Tweening;
using UnityEngine;

/// <summary>
/// Перемещает Transform к абсолютной мировой позиции.
/// </summary>
public class DoTweenMovementMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3 movement;

    /// <summary>
    /// Пересоздаёт анимацию перемещения.
    /// </summary>
    public override Tween CreateAnimation()
    {
        tween?.Kill();

        tween = transform.DOMove(movement, duration)
            .SetLoops(loopCount, loopType)
            .SetEase(ease);
        IsCreated = true;

        return tween;
    }
}
