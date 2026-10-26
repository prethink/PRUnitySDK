using UnityEngine;

/// <summary>
/// Замедляет или ускоряет отдельное физическое тело относительно остальной сцены.
/// <para>
/// Темп мира физика учитывает сама — он заложен в шаг симуляции, — а слои <see cref="PRTimeScale"/>
/// PhysX не поддерживает: симуляция одна на сцену. База добавляет телу разницу между его слоями
/// и миром, слои задаёт наследник.
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
    /// Слои времени этого тела. Только <see cref="TimeLayer.World"/> — тело идёт в темпе мира, и драйвер
    /// бездействует: этот темп уже заложен в шаг симуляции.
    /// </summary>
    protected abstract TimeLayer GetTimeLayers();

    /// <summary>
    /// Насколько тело идёт медленнее или быстрее мира.
    /// <para>
    /// Это отношение масштаба тела к масштабу мира: замедление мира уже заложено
    /// в шаг симуляции, и учитывать его второй раз нельзя.
    /// </para>
    /// </summary>
    public float RelativeTimeScale => PRTimeScale.Instance.RelativeToWorld(GetTimeLayers());

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

    /// <remarks>
    /// Пересчёт при любом изменении, а не только своих слоёв: темп тела относителен к миру, и замедление мира
    /// его тоже меняет. Сам пересчёт ничего не делает, если масштаб не изменился.
    /// </remarks>
    public void OnPRTimeScaleChange(TimeLayer layers)
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
