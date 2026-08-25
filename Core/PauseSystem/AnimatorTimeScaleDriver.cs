using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Тикает аниматор вручную игровым временем - так же, как хост тикает физику
/// через <c>Physics.Simulate(GameFixedDeltaTime)</c>.
/// <para>
/// Аниматор выводится из автоматического обновления Unity (<c>animator.enabled = false</c>),
/// а его продвижение выполняет <see cref="PRUpdate"/>: пауза - это просто отсутствие
/// вызова, замедление - меньший шаг времени. Ни скорость, ни состояние никуда
/// не сохраняются, поэтому нечего затирать и восстанавливать.
/// </para>
/// <para>
/// Альтернатива - <see cref="AnimatorPauseMonitor"/>, который оставляет обновление
/// за Unity и играет со скоростью аниматора. Оба подхода сосуществуют: аниматор
/// с этим драйвером монитор не трогает.
/// </para>
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorTimeScaleDriver : PRMonoBehaviour
{
    #region Поля и свойства

    [Header("Аниматор")]
    [Tooltip("Аниматор, которым управляет драйвер. Если пусто - берётся с этого объекта.")]
    [SerializeField] private Animator animator;

    [Tooltip("Слой масштаба времени. Пусто - используется глобальный масштаб.")]
    [SerializeField] private EnumerationReference<PRTimeScaleEnumerationProvider> timeScaleLayer;

    [Tooltip("Возвращать аниматору автоматическое обновление при выключении компонента. " +
        "Включайте для объектов из пула, чтобы аниматор не остался замороженным.")]
    [SerializeField] private bool restoreOnDisable = true;

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

        animator.Update(deltaTime);
    }

    #endregion

    #region Методы

    /// <summary>
    /// Текущий масштаб времени для аниматора с учётом слоя.
    /// </summary>
    public float GetTimeScale()
    {
        var layer = timeScaleLayer != null ? timeScaleLayer.ToEnumeration() : null;

        return PRTimeScale.Instance.Resolve(layer);
    }

    /// <summary>
    /// Задать слой масштаба времени в рантайме.
    /// </summary>
    public void SetTimeScaleLayer(Enumeration layer)
    {
        timeScaleLayer ??= new EnumerationReference<PRTimeScaleEnumerationProvider>();

        if (layer != null)
            timeScaleLayer.Set(layer);
    }

    private void Attach()
    {
        if (animator == null)
            return;

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
