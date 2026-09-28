using UnityEngine;

/// <summary>
/// Продвигает частицы вручную игровым временем; пауза останавливает их вместе с игрой.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public sealed class ParticleTimeScaleDriver : PRMonoBehaviour
{
    private ParticleSystem target;
    private bool playOnAwake;
    private bool resumeAutomatic;

    protected override void OnEnable()
    {
        target = GetComponent<ParticleSystem>();
        var main = target.main;
        playOnAwake = main.playOnAwake;
        resumeAutomatic = target.isPlaying || playOnAwake;
        main.playOnAwake = false;
        if (resumeAutomatic && target.isStopped) target.Play(false);
        target.Pause(false);
        base.OnEnable();
    }

    protected override void PRUpdate()
    {
        base.PRUpdate();
        Advance(PRTime.Instance.GameDeltaTime);
    }

    /// <summary>
    /// Шаг ручной симуляции. Нулевой шаг сохраняет частицы и время эффекта без изменений.
    /// </summary>
    public void Advance(float gameDeltaTime)
    {
        if (target == null) target = GetComponent<ParticleSystem>();
        if (target == null || gameDeltaTime <= 0f || !target.gameObject.activeInHierarchy) return;
        // У дочерних систем свой драйвер: общий рекурсивный шаг продвинул бы их дважды.
        target.Simulate(gameDeltaTime, false, false, false);
    }

    protected override void OnDisable()
    {
        if (target != null)
        {
            var main = target.main;
            main.playOnAwake = playOnAwake;
            if (resumeAutomatic) target.Play(false);
        }
        base.OnDisable();
    }
}
