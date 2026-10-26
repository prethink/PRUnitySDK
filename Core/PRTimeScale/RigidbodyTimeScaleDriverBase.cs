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
    /// Скорости тела в обычном темпе на момент заморозки: при нуле их не из чего восстановить делением.
    /// </summary>
    private Vector3 frozenVelocity;

    private Vector3 frozenAngularVelocity;

    /// <summary>
    /// Ограничения тела до заморозки: на время заморозки драйвер ставит <see cref="RigidbodyConstraints.FreezeAll"/>.
    /// </summary>
    private RigidbodyConstraints savedConstraints;

    private bool locked;

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

    /// <summary>
    /// Тело заморожено: его темп — ноль.
    /// </summary>
    public bool IsFrozen => appliedScale <= Mathf.Epsilon;

    #endregion

    #region MonoBehaviour

    protected override void InitializationComponents()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();

        appliedScale = RelativeTimeScale;

        // Тело появилось уже в заморозке — фиксируем сразу, а скорость, которую ему выдадут, заберёт HoldFrozen.
        if (IsFrozen && scaleVelocityOnChange)
            LockBody();

        base.InitializationComponents();
    }

    /// <summary>
    /// Прикладывает поправку к гравитации. На логической паузе PRMonoBehaviour
    /// не вызывает этот метод, поэтому тело не получает лишних сил.
    /// </summary>
    protected override void PRFixedUpdate()
    {
        base.PRFixedUpdate();

        HoldFrozen();

        if (!compensateGravity || body == null || !body.useGravity || body.isKinematic)
            return;

        var scale = RelativeTimeScale;

        if (Mathf.Approximately(scale, 1f))
            return;

        // PhysX уже приложил g за этот шаг, а телу нужно g·k² - добавляем разницу.
        body.AddForce(Physics.gravity * (scale * scale - 1f), ForceMode.Acceleration);
    }

    #endregion

    /// <summary>
    /// Включённый снова драйвер догоняет масштаб, сменившийся без него, и фиксирует тело, если оно заморожено.
    /// </summary>
    /// <remarks>
    /// Выключенный драйвер событий не получает и ограничения при выключении снимает, поэтому без этого тело,
    /// включённое посреди заморозки, двигалось бы свободно. Сохранённая скорость заморозки при этом не сбрасывается:
    /// объекту из пула скорость задаёт код появления, и она перекрывает старую.
    /// </remarks>
    protected override void OnEnable()
    {
        base.OnEnable();

        if (body == null)
            return;

        ApplyScaleChange();

        if (IsFrozen && scaleVelocityOnChange)
            LockBody();
    }

    /// <summary>
    /// Выключенный драйвер (объект ушёл в пул) не держит тело замороженным: ограничения возвращаются.
    /// </summary>
    protected override void OnDisable()
    {
        UnlockBody();
        base.OnDisable();
    }

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

        // Замороженному импульс достаётся после разморозки: время для него стоит, а выстрел не должен пропасть.
        // Непрерывная сила за нулевое время ничего не меняет.
        if (IsFrozen)
        {
            if (mode == ForceMode.VelocityChange)
                frozenVelocity += force;
            else if (mode == ForceMode.Impulse && body.mass > 0f)
                frozenVelocity += force / body.mass;
            return;
        }

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
    /// Задать скорость тела в его темпе: замедленное вдвое летит вдвое медленнее, замороженное получит её после
    /// разморозки.
    /// </summary>
    public void SetScaledVelocity(Vector3 velocity)
    {
        if (body == null)
            return;

        if (IsFrozen)
            frozenVelocity = velocity;
        else
            body.velocity = velocity * RelativeTimeScale;
    }

    /// <summary>
    /// Пока тело заморожено, скорость, выданную ему мимо драйвера (появилось уже в заморозке, задали напрямую,
    /// толкнули), забирает себе: тело стоит, а после разморозки движется с ней в новом темпе.
    /// </summary>
    private void HoldFrozen()
    {
        if (!IsFrozen || !scaleVelocityOnChange || body == null || body.isKinematic)
            return;

        if (body.velocity.sqrMagnitude <= 0f && body.angularVelocity.sqrMagnitude <= 0f)
            return;

        frozenVelocity += body.velocity;
        frozenAngularVelocity += body.angularVelocity;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Фиксирует замороженное тело: нулевая скорость не мешает PhysX сдвигать его толчками и столкновениями,
    /// а с ограничениями оно стоит на месте, и другие тела от него отскакивают.
    /// </summary>
    private void LockBody()
    {
        if (locked || body == null || body.isKinematic)
            return;

        savedConstraints = body.constraints;
        body.constraints = RigidbodyConstraints.FreezeAll;
        locked = true;
    }

    /// <summary>
    /// Возвращает телу ограничения, что были до заморозки.
    /// </summary>
    private void UnlockBody()
    {
        if (!locked)
            return;

        locked = false;
        if (body != null)
            body.constraints = savedConstraints;
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

        bool wasFrozen = appliedScale <= Mathf.Epsilon;
        bool freezes = scale <= Mathf.Epsilon;

        if (!wasFrozen && freezes)
        {
            // Замирает: скорость в обычном темпе запоминаем, иначе после оттаивания тело стояло бы — из нуля
            // делением прежнюю скорость не восстановить.
            frozenVelocity = body.velocity / appliedScale;
            frozenAngularVelocity = body.angularVelocity / appliedScale;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            LockBody();
        }
        else if (wasFrozen && !freezes)
        {
            // Оттаивает: летящее продолжает полёт в новом темпе.
            UnlockBody();
            body.velocity = frozenVelocity * scale;
            body.angularVelocity = frozenAngularVelocity * scale;
            frozenVelocity = Vector3.zero;
            frozenAngularVelocity = Vector3.zero;
        }
        else if (!wasFrozen)
        {
            // Пересчёт идёт от ранее применённого масштаба, а не от единицы: иначе
            // повторные изменения накапливали бы ошибку.
            var ratio = scale / appliedScale;
            body.velocity *= ratio;
            body.angularVelocity *= ratio;
        }

        appliedScale = scale;
    }

    #endregion
}
