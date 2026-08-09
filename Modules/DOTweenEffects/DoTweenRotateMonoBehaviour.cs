using DG.Tweening;
using UnityEngine;

/// <summary>
/// Вращает Transform к указанным углам Эйлера.
/// </summary>
public class DoTweenRotateMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private Vector3 rotateCoordinate;
    [SerializeField] private RotateMode rotateMode;

    /// <summary>
    /// Устанавливает режим интерпретации вращения DOTween.
    /// </summary>
    public DoTweenRotateMonoBehaviour SetRotateMode(RotateMode rotateMode)
    {
        this.rotateMode = rotateMode;
        return this;
    }

    /// <summary>
    /// Устанавливает целевые углы Эйлера.
    /// </summary>
    public DoTweenRotateMonoBehaviour SetRotateCoordinate(Vector3 rotateCoordinate)
    {
        this.rotateCoordinate = rotateCoordinate;
        return this;
    }

    /// <summary>
    /// Пересоздаёт анимацию вращения.
    /// </summary>
    public override Tween CreateAnimation()
    {
        tween?.Kill();

        tween = transform.DORotate(rotateCoordinate, duration, rotateMode)
            .SetLoops(loopCount, loopType)
            .SetEase(ease);
        IsCreated = true;

        return tween;
    }
}
