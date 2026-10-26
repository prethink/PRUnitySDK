using UnityEngine;

/// <summary>
/// Продвигает частицы вручную игровым временем; пауза останавливает их вместе с игрой.
/// </summary>
/// <remarks>
/// Шаг — время своих слоёв (<see cref="PRTimeScale"/>): частицы в слое <see cref="TimeLayer.Effects"/> можно
/// замедлять отдельно от мира или, наоборот, оставлять в обычном темпе.
/// </remarks>
[RequireComponent(typeof(ParticleSystem))]
public sealed class ParticleTimeScaleDriver : PRMonoBehaviour
{
    [Tooltip("Слои времени частиц: какие замедления их задевают. По умолчанию — мир.")]
    [SerializeField] private TimeLayer timeLayers = TimeLayer.World;

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
        Advance(PRTime.Instance.RealDeltaTime * PRTimeScale.Instance.Resolve(timeLayers));
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

    /// <summary>
    /// Задать слои времени в рантайме.
    /// </summary>
    public void SetTimeLayers(TimeLayer layers)
    {
        timeLayers = layers;
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
