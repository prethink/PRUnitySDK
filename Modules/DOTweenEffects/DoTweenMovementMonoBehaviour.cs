using DG.Tweening;
using UnityEngine;

public class DoTweenMovementMonoBehaviour : DoTweenBaseEffectMonoBehaviour
{
    private Vector3 startPosition;

    [Header("Rotate")]
    [SerializeField] private Vector3 movement;

    public override Tween CreateAnimation()
    {
        IsCreated = true;
        tween?.Kill();
        if(startPosition == Vector3.zero)
            startPosition = gameObject.transform.position;

        tween = transform.DOMove(movement, 1 / duration)
            .SetEase(ease);

        return tween;
    }
}
