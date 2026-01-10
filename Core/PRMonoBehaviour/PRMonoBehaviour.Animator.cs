using System.Collections.Generic;
using UnityEngine;

public partial class PRMonoBehaviour
{
    protected class AnimatorData
    {
        public float Speed;
        public bool Enabled;
    }

    protected readonly HashSet<Animator> animators = new();

    protected readonly Dictionary<Animator, AnimatorData> animatorStates = new();

    [MethodHook(MethodHookStage.Pause)]
    public virtual void OnPauseAnimatorChange()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            PauseAnimators();
        else
            ResumeAnimators();
    }

    protected void RegisterAnimator(Animator animator)
    {
        if (animator == null)
            return;

        animators.Add(animator);
        OnPauseStateChanged(new PauseEventArgs()); // если сразу надо применить
    }

    private void PauseAnimators()
    {
        foreach (var animator in animators)
        {
            if (animator == null || animatorStates.ContainsKey(animator))
                continue;

            animatorStates[animator] = new AnimatorData
            {
                Speed = animator.speed,
                Enabled = animator.enabled
            };

            animator.speed = 0f;
        }
    }

    private void ResumeAnimators()
    {
        foreach (var pair in animatorStates)
        {
            var animator = pair.Key;
            var data = pair.Value;

            if (animator == null)
                continue;

            animator.speed = data.Speed;
        }

        animatorStates.Clear();
    }
}
