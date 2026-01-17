using DG.Tweening;
using UnityEngine;

public interface IDoTweenEffect : IPauseStateListener
{
    public Ease Ease { get; }

    public LoopType LoopType { get; }

    public int LoopCount { get; }

    public float Duration { get; }

    public bool PlayAnimationOnStart { get; }

    public Tween CreateAnimation();

    public void StartAnimation();

    public void StopAnimation();

    public void DestroyAnimation();
}
