using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Тикает аниматор вручную игровым временем, как хост тикает физику через
/// <c>Physics.Simulate(GameFixedDeltaTime)</c>.
/// </summary>
/// <remarks>
/// Аниматор выведен из автоматического обновления Unity (<c>animator.enabled = false</c>),
/// продвигает его <see cref="PRUpdate"/>: пауза — это отсутствие вызова, замедление —
/// меньший шаг. Скорость и состояние никуда не сохраняются.
/// <para>
/// Альтернатива — <see cref="AnimatorPauseMonitor"/>, он оставляет обновление за Unity
/// и меняет скорость аниматора. Аниматор с этим драйвером монитор не трогает.
/// </para>
/// </remarks>
[RequireComponent(typeof(Animator))]
public class AnimatorTimeScaleDriver : PRMonoBehaviour, IEntityLodListener
{
    #region Поля и свойства

    [Header("Аниматор")]
    [Tooltip("Аниматор, которым управляет драйвер. Если пусто - берётся с этого объекта.")]
    [SerializeField] private Animator animator;

    [Tooltip("Слои времени аниматора: какие замедления его задевают. По умолчанию — мир.")]
    [SerializeField] private TimeLayer timeLayers = TimeLayer.World;

    [Tooltip("Возвращать аниматору автоматическое обновление при выключении компонента. " +
        "Включайте для объектов из пула, чтобы аниматор не остался замороженным.")]
    [SerializeField] private bool restoreOnDisable = true;

    [Tooltip("Не продвигать аниматор, пока сущность вне кадра и дальше ближней зоны камеры (EntityLod → Hidden). " +
        "Как Cull Completely у обычного аниматора: поза замирает и продолжается, когда сущность снова в кадре. " +
        "Действует только у сущностей с детализацией (EntityLod) — NPC, ботов, питомцев. " +
        "Выключайте, если игра читает позу костей или ждёт событий анимации, когда объекта не видно.")]
    [SerializeField] private bool pauseWhenInvisible = true;

    /// <summary>
    /// Аниматор принадлежит сущности с детализацией (<see cref="EntityLod"/>).
    /// </summary>
    /// <remarks>
    /// Только тогда видимость решает, тикать ли. Видимость считает <see cref="EntityLodService"/> по сфере сущности,
    /// а не по мешам: меш, нарисованный в обход обычной отрисовки, Unity видимым не считает (руки от первого лица
    /// рисует проход Render Objects, и с проверкой у всех аниматоров они замирали), а меши, добавленные позже
    /// (смена вида, объект из пула), список, собранный один раз, не видел бы.
    /// </remarks>
    private bool managedByLod;

    /// <summary>
    /// Сущность вне кадра и дальше ближней зоны (<see cref="EntityLodLevel.Hidden"/>).
    /// </summary>
    private bool lodHidden;

    /// <summary>
    /// Раз в сколько кадров продвигать аниматор: у дальних в кадре реже (<see cref="EntityLodLevel.Reduced"/>).
    /// </summary>
    private int frameStep = 1;

    /// <summary>
    /// Кадров с прошлого тика.
    /// </summary>
    private int skippedFrames;

    /// <summary>
    /// Время, накопленное за пропущенные кадры: анимация идёт реже, но не медленнее.
    /// </summary>
    private float pendingDeltaTime;

    /// <summary>
    /// Аниматор хоть раз продвинут после включения.
    /// </summary>
    /// <remarks>
    /// Видимость Unity узнаёт, только отрисовав меш. Не продвигай аниматор до первого кадра — новый объект
    /// показался бы кадр в позе привязки.
    /// </remarks>
    private bool hasEvaluated;

    /// <summary>
    /// Аниматоры, которыми управляют драйверы. Монитор паузы пропускает их:
    /// иначе он обнулил бы скорость и запомнил снимок, конфликтуя с ручным тиком.
    /// </summary>
    private static readonly HashSet<Animator> managedAnimators = new();

    /// <summary>
    /// Управляет ли аниматором драйвер ручного тика.
    /// </summary>
    public static bool IsManaged(Animator animator)
    {
        return animator != null && managedAnimators.Contains(animator);
    }

    /// <summary>
    /// Аниматор под управлением драйвера.
    /// </summary>
    public Animator Animator => animator;

    #endregion

    #region MonoBehaviour

    protected override void InitializationComponents()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        base.InitializationComponents();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Attach();
    }

    protected override void OnDisable()
    {
        Detach();
        base.OnDisable();
    }

    /// <summary>
    /// Продвигает анимацию игровым временем. На логической паузе PRMonoBehaviour
    /// не вызывает этот метод, поэтому анимация останавливается сама.
    /// </summary>
    protected override void PRUpdate()
    {
        base.PRUpdate();

        if (animator == null || !animator.gameObject.activeInHierarchy)
            return;

        var deltaTime = PRTime.Instance.RealDeltaTime * GetTimeScale();

        if (deltaTime <= 0f)
            return;

        if (pauseWhenInvisible && managedByLod && lodHidden && hasEvaluated)
        {
            pendingDeltaTime = 0f;
            return;
        }

        pendingDeltaTime += deltaTime;

        if (hasEvaluated && ++skippedFrames < frameStep)
            return;

        skippedFrames = 0;
        animator.Update(pendingDeltaTime);
        pendingDeltaTime = 0f;
        hasEvaluated = true;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Текущий масштаб времени для аниматора с учётом его слоёв.
    /// </summary>
    public float GetTimeScale()
    {
        return PRTimeScale.Instance.Resolve(timeLayers);
    }

    /// <summary>
    /// Задать слои времени в рантайме.
    /// </summary>
    public void SetTimeLayers(TimeLayer layers)
    {
        timeLayers = layers;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Сущность с детализацией: вне кадра вдали аниматор стоит, дальние в кадре тикают раз в
    /// <see cref="EntityLodSettings.ReducedAnimationStep"/> кадров. Чтобы дальние не тикали все в один кадр,
    /// счёт у каждого начинается со своего места.
    /// </remarks>
    public void OnEntityLodChanged(EntityLod lod)
    {
        managedByLod = lod.isActiveAndEnabled;
        lodHidden = managedByLod && lod.Level == EntityLodLevel.Hidden;

        EntityLodSettings settings = PRUnitySDK.Settings != null ? PRUnitySDK.Settings.EntityLod : null;
        int step = managedByLod && lod.Level == EntityLodLevel.Reduced && settings != null
            ? Mathf.Max(1, settings.ReducedAnimationStep)
            : 1;

        if (step == frameStep)
            return;

        frameStep = step;
        skippedFrames = Random.Range(0, step);
    }

    private void Attach()
    {
        if (animator == null)
            return;

        hasEvaluated = false;

        managedAnimators.Add(animator);

        // Снимаем аниматор с автообновления: дальше его продвигает только этот драйвер.
        animator.enabled = false;
    }

    private void Detach()
    {
        if (animator == null)
            return;

        managedAnimators.Remove(animator);

        if (restoreOnDisable)
            animator.enabled = true;
    }

    #endregion
}
