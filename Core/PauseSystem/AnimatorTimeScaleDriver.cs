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
public class AnimatorTimeScaleDriver : PRMonoBehaviour
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

    [Tooltip("Не продвигать аниматор, пока ни один меш под ним не виден ни одной камере. Как Cull Completely " +
        "у обычного аниматора: вне кадра поза замирает и продолжается, когда объект снова в кадре. " +
        "Действует только у сущностей с детализацией (EntityLod) — NPC, ботов, питомцев. " +
        "Выключайте, если игра читает позу костей или ждёт событий анимации, когда объекта не видно.")]
    [SerializeField] private bool pauseWhenInvisible = true;

    /// <summary>
    /// Аниматор принадлежит сущности с детализацией (<see cref="EntityLod"/>).
    /// </summary>
    /// <remarks>
    /// Только тогда видимость решает, тикать ли. Меш, нарисованный в обход обычной отрисовки, Unity видимым
    /// не считает: руки от первого лица рисует проход Render Objects, и с проверкой у всех аниматоров они
    /// замирали.
    /// </remarks>
    private bool managedByLod;

    /// <summary>
    /// Меши под аниматором, по которым решается, виден ли он.
    /// </summary>
    private Renderer[] renderers;

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

        if (pauseWhenInvisible && managedByLod && hasEvaluated && !IsVisible())
            return;

        animator.Update(deltaTime);
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

    /// <summary>
    /// Виден ли хоть один меш под аниматором какой-нибудь камере.
    /// </summary>
    /// <remarks>
    /// Меши собираются один раз; если какой-то из них уничтожили (сменили вид), список собирается заново.
    /// Без мешей аниматор считается видимым: решать не по чему.
    /// </remarks>
    private bool IsVisible()
    {
        renderers ??= animator.GetComponentsInChildren<Renderer>(true);

        bool lost = false;

        foreach (Renderer meshRenderer in renderers)
        {
            if (meshRenderer == null)
            {
                lost = true;
                continue;
            }

            if (meshRenderer.isVisible)
                return true;
        }

        if (lost)
            renderers = null;

        return renderers != null && renderers.Length == 0;
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
