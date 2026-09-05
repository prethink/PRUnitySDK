using UnityEngine;

/// <summary>
/// Замедляет или ускоряет отдельное физическое тело относительно остальной сцены.
/// <para>
/// Глобальный масштаб физика учитывает сама, а слои <see cref="PRTimeScale"/> PhysX
/// не поддерживает: симуляция одна на сцену. База добавляет телу разницу между его слоем
/// и глобальным темпом, слой задаёт наследник.
/// </para>
/// <para>
/// При замедлении в k раз скорость умножается на k, а ускорение на k², потому что путь
/// равен v·(kt) + g·(kt)²/2. Без поправки к гравитации персонаж взлетал бы замедленно,
/// а падал в обычном темпе, поэтому телу прикладывается ускорение g·(k²-1), равное нулю
/// при k = 1.
/// </para>
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class RigidbodyTimeScaleDriverBase : PRMonoBehaviour, IOnPRTimeScaleChange
{
    #region Поля и свойства

    [Header("Физика")]
    [Tooltip("Тело, которым управляет драйвер. Если пусто - берётся с этого объекта.")]
    [SerializeField] protected Rigidbody body;

    [Tooltip("Компенсировать гравитацию. Выключайте для тел, которым гравитация не важна.")]
    [SerializeField] protected bool compensateGravity = true;

    [Tooltip("Масштабировать скорости в момент смены масштаба. Без этого уже летящее тело " +
        "продолжит движение в прежнем темпе до следующего толчка.")]
    [SerializeField] protected bool scaleVelocityOnChange = true;

    /// <summary>
    /// Масштаб, применённый к скоростям тела в последний раз.
    /// </summary>
    private float appliedScale = 1f;

    /// <summary>
    /// Слой масштаба времени для этого тела. Null означает глобальный темп -
    /// в этом случае драйвер бездействует, потому что глобальный масштаб уже
    /// заложен в шаг симуляции.
    /// </summary>
    protected abstract Enumeration GetTimeScaleLayer();

    /// <summary>
    /// Насколько тело идёт медленнее или быстрее остальной сцены.
    /// <para>
    /// Это отношение масштаба слоя к глобальному: глобальное замедление уже заложено
    /// в шаг симуляции, и учитывать его второй раз нельзя.
    /// </para>
    /// </summary>
    public float RelativeTimeScale
    {
        get
        {
            var globalScale = PRTimeScale.Instance.GetGlobalTimeScale();

            if (globalScale <= Mathf.Epsilon)
                return 1f;

            var layer = GetTimeScaleLayer();

            if (layer == null)
                return 1f;

            return PRTimeScale.Instance.Resolve(layer) / globalScale;
        }
    }

    /// <summary>
    /// Тело под управлением драйвера.
    /// </summary>
    public Rigidbody Body => body;

    #endregion

    #region MonoBehaviour

    protected override void InitializationComponents()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();

        appliedScale = RelativeTimeScale;

        base.InitializationComponents();
    }

    /// <summary>
    /// Прикладывает поправку к гравитации. На логической паузе PRMonoBehaviour
    /// не вызывает этот метод, поэтому тело не получает лишних сил.
    /// </summary>
    protected override void PRFixedUpdate()
    {
        base.PRFixedUpdate();

        if (!compensateGravity || body == null || !body.useGravity || body.isKinematic)
            return;

        var scale = RelativeTimeScale;

        if (Mathf.Approximately(scale, 1f))
            return;

        // PhysX уже приложил g за этот шаг, а телу нужно g·k² - добавляем разницу.
        body.AddForce(Physics.gravity * (scale * scale - 1f), ForceMode.Acceleration);
    }

    #endregion

    #region IOnPRTimeScaleChange

    public void OnPRTimeScaleChange(Enumeration enumeration, float value)
    {
        ApplyScaleChange();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Приложить к телу силу с учётом его масштаба времени.
    /// <para>
    /// Импульс и мгновенное изменение скорости масштабируются на k, непрерывная сила
    /// и ускорение - на k²: они действуют в течение времени, которое для тела течёт
    /// медленнее.
    /// </para>
    /// </summary>
    public void AddScaledForce(Vector3 force, ForceMode mode)
    {
        if (body == null)
            return;

        var scale = RelativeTimeScale;

        var scaledForce = mode switch
        {
            ForceMode.Impulse => force * scale,
            ForceMode.VelocityChange => force * scale,
            _ => force * (scale * scale)
        };

        body.AddForce(scaledForce, mode);
    }

    /// <summary>
    /// Пересчитать скорость тела под новый масштаб времени.
    /// Вызывается автоматически при изменении масштаба.
    /// </summary>
    public void ApplyScaleChange()
    {
        if (!scaleVelocityOnChange || body == null || body.isKinematic)
            return;

        var scale = RelativeTimeScale;

        if (Mathf.Approximately(scale, appliedScale))
            return;

        // Пересчёт идёт от ранее применённого масштаба, а не от единицы: иначе
        // повторные изменения накапливали бы ошибку.
        if (appliedScale > Mathf.Epsilon)
        {
            var ratio = scale / appliedScale;
            body.velocity *= ratio;
            body.angularVelocity *= ratio;
        }

        appliedScale = scale;
    }

    #endregion
}
