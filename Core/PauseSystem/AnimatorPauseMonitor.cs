using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Останавливает аниматоры объекта на время логической паузы и возвращает им
/// прежнюю скорость после её снятия.
/// <para>
/// Пока пауза активна, реальная скорость аниматора равна нулю, а исходное значение
/// лежит в снимке. Поэтому менять скорость напрямую в это время нельзя - при
/// возобновлении она будет затёрта снимком. Для изменения скорости используйте
/// <see cref="SetSpeed"/>: он пишет в снимок, если пауза активна, и в аниматор,
/// если нет.
/// </para>
/// </summary>
public class AnimatorPauseMonitor : MonoBehaviour, IPauseStateListener
{
    protected class AnimatorData
    {
        public float Speed;
    }

    /// <summary>
    /// Мониторы по аниматорам. Нужен, чтобы код, задающий скорость, мог узнать,
    /// находится ли аниматор под управлением паузы, не разыскивая монитор в иерархии.
    /// </summary>
    private static readonly Dictionary<Animator, AnimatorPauseMonitor> monitors = new();

    protected readonly HashSet<Animator> animators = new();

    protected readonly Dictionary<Animator, AnimatorData> animatorStates = new();

    #region MonoBehaviour

    private void OnEnable()
    {
        EventBus.Subscribe(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    private void Awake()
    {
        var animators = this.gameObject.GetComponentsInSelfOrChildren<Animator>();
        foreach (var animator in animators)
            RegisterAnimator(animator);
    }

    private void OnDestroy()
    {
        foreach (var animator in animators)
        {
            if (animator != null && monitors.TryGetValue(animator, out var monitor) && monitor == this)
                monitors.Remove(animator);
        }
    }

    #endregion

    #region Методы

    public void RegisterAnimator(Animator animator)
    {
        if (animator == null)
            return;

        animators.Add(animator);
        monitors[animator] = this;
        OnPauseStateChanged(new PauseStateEventArgs()); // если сразу надо применить
    }

    /// <summary>
    /// Задать скорость аниматора с учётом паузы.
    /// <para>
    /// Во время логической паузы значение сохраняется в снимок и применится при
    /// возобновлении. Прямая запись animator.speed в этот момент терялась бы:
    /// ResumeAnimators вернул бы скорость, запомненную до паузы.
    /// </para>
    /// </summary>
    /// <param name="animator">Аниматор.</param>
    /// <param name="speed">Новая скорость.</param>
    public static void SetSpeed(Animator animator, float speed)
    {
        if (animator == null)
            return;

        // Скоростью аниматора с ручным тиком управляет драйвер через шаг времени -
        // здесь она всегда должна оставаться единицей.
        if (AnimatorTimeScaleDriver.IsManaged(animator))
            return;

        if (monitors.TryGetValue(animator, out var monitor)
            && monitor != null
            && monitor.animatorStates.TryGetValue(animator, out var data))
        {
            data.Speed = speed;
            return;
        }

        animator.speed = speed;
    }

    private void PauseAnimators()
    {
        foreach (var animator in animators)
        {
            if (animator == null || animatorStates.ContainsKey(animator))
                continue;

            // Аниматор с ручным тиком останавливается сам - его драйвер просто
            // не вызывается на паузе. Обнулять скорость и запоминать снимок нельзя:
            // после возобновления снимок затёр бы значение, выставленное драйвером.
            if (AnimatorTimeScaleDriver.IsManaged(animator))
                continue;

            animatorStates[animator] = new AnimatorData
            {
                Speed = animator.speed,
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

    #endregion

    #region IPauseStateListener

    public void OnPauseStateChanged(PauseStateEventArgs args)
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            PauseAnimators();
        else
            ResumeAnimators();
    }

    #endregion
}
