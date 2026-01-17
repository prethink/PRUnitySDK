using DG.Tweening;
using UnityEngine;

public class DoTweenRotateMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    [Header("Rotate")]
    [SerializeField] private Vector3 rotateCoordinate;
    [SerializeField] private RotateMode rotateMode;

    public DoTweenRotateMonoBehaviour SetRotateMode(RotateMode rotateMode)
    {
        this.rotateMode = rotateMode;
        return this;
    }

    public DoTweenRotateMonoBehaviour SetRotateCoordinate(Vector3 rotateCoordinate)
    {
        this.rotateCoordinate = rotateCoordinate;
        return this;
    }

    public override Tween CreateAnimation()
    {
        IsCreated = true;
        tween?.Kill();

        tween = transform.DORotate(rotateCoordinate, 1 / duration, rotateMode)
            .SetLoops(loopCount, loopType)
            .SetEase(ease);

        return tween;
    }
}
