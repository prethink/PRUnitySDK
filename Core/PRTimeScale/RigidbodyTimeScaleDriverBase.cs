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
        "продолжит движение в прежнем темпе до следующего толчка. Заморозку не отключает: " +
        "при нулевом темпе тело стоит всегда.")]
    [SerializeField] protected bool scaleVelocityOnChange = true;

    /// <summary>
    /// Масштаб, применённый к скоростям тела в последний раз.
    /// </summary>
    private float appliedScale = 1f;

    /// <summary>
    /// Скорости тела в обычном темпе, придержанные на время заморозки: при нуле их не из чего восстановить
    /// делением. При разморозке умножаются на новый темп.
    /// </summary>
    private Vector3 frozenVelocity;

    private Vector3 frozenAngularVelocity;

    /// <summary>
    /// Скорости, с которыми тело замерло при выключенном <see cref="scaleVelocityOnChange"/>: возвращаются как есть.
    /// Заморозка с разморозкой должны давать то же, что прямая смена масштаба, а без пересчёта она скорость не меняет.
    /// </summary>
    private Vector3 keptVelocity;

    private Vector3 keptAngularVelocity;

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
    /// Тело заморожено: его темп — ноль. Такое тело стоит на месте при любых настройках, а толчки получает после
    /// разморозки.
    /// </summary>
    public bool IsFrozen => appliedScale <= Mathf.Epsilon;

    #endregion

    #region MonoBehaviour

    protected override void InitializationComponents()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();

        // Фиксирует тело OnEnable: драйвер, выключенный с самого начала, тело не держит.
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
    /// Включённый драйвер начинает с текущего масштаба, как новый, и фиксирует тело, если оно заморожено.
    /// </summary>
    /// <remarks>
    /// Скорость тела при этом не пересчитывается: её мог задать код появления до этого OnEnable, и задал он её
    /// уже под нынешний темп. Замороженному телу её придержит <see cref="HoldFrozen"/> и вернёт при разморозке.
    /// </remarks>
    protected override void OnEnable()
    {
        base.OnEnable();

        if (body == null)
            return;

        appliedScale = RelativeTimeScale;

        if (IsFrozen)
            LockBody();
    }

    /// <summary>
    /// Выключенный драйвер (объект ушёл в пул) не держит тело замороженным: ограничения возвращаются, а скорость,
    /// придержанная на время заморозки, забывается.
    /// </summary>
    /// <remarks>
    /// Unity и сама обнуляет скорость выключенного тела. Без сброса объект из пула, включённый снова и размороженный,
    /// полетел бы со скоростью прошлой жизни.
    /// </remarks>
    protected override void OnDisable()
    {
        UnlockBody();
        ForgetHeldVelocity();
        base.OnDisable();
    }

    #region IOnPRTimeScaleChange

    /// <remarks>
    /// Пересчёт при любом изменении, а не только своих слоёв: темп тела относителен к миру, и замедление мира
    /// его тоже меняет. Сам пересчёт ничего не делает, если масштаб не изменился.
    /// <para>
    /// Подписка живёт от Awake до OnDestroy, поэтому событие приходит и выключенному драйверу. Его он пропускает:
    /// иначе разморозка, случившаяся, пока объект в пуле, записала бы телу нулевую скорость, а заморозка — зафиксировала
    /// бы его. Масштаб включённый драйвер возьмёт в OnEnable.
    /// </para>
    /// </remarks>
    public void OnPRTimeScaleChange(TimeLayer layers)
    {
        if (isActiveAndEnabled)
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
        {
            frozenVelocity = velocity;
            keptVelocity = Vector3.zero;
        }
        else
            body.velocity = velocity * RelativeTimeScale;
    }

    /// <summary>
    /// Пока тело заморожено, скорость, выданную ему мимо драйвера, забирает себе: тело стоит, а после разморозки
    /// движется с ней — в новом темпе или, без <see cref="scaleVelocityOnChange"/>, как есть: тело, которому её задали
    /// без заморозки, тоже не пересчитали бы.
    /// </summary>
    /// <remarks>
    /// Ловит скорость, выданную до фиксации (тело включили посреди заморозки — зовёт <see cref="LockBody"/>), и у тела,
    /// которое не зафиксировано, потому что замерло кинематическим. Зафиксированному телу Unity скорость не задаёт
    /// вовсе: <see cref="RigidbodyConstraints.FreezeAll"/> её отбрасывает, поэтому замороженному скорость дают
    /// <see cref="SetScaledVelocity"/> и <see cref="AddScaledForce"/>.
    /// </remarks>
    private void HoldFrozen()
    {
        if (!IsFrozen || body == null || body.isKinematic)
            return;

        if (body.velocity.sqrMagnitude <= 0f && body.angularVelocity.sqrMagnitude <= 0f)
            return;

        if (scaleVelocityOnChange)
        {
            frozenVelocity += body.velocity;
            frozenAngularVelocity += body.angularVelocity;
        }
        else
        {
            keptVelocity += body.velocity;
            keptAngularVelocity += body.angularVelocity;
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Фиксирует замороженное тело: нулевая скорость не мешает PhysX сдвигать его толчками и столкновениями,
    /// а с ограничениями оно стоит на месте, и другие тела от него отскакивают.
    /// </summary>
    /// <remarks>
    /// <see cref="RigidbodyConstraints.FreezeAll"/> обнуляет скорость тела, поэтому выданную ему до фиксации
    /// (тело включили посреди заморозки) сначала забирает <see cref="HoldFrozen"/>.
    /// </remarks>
    private void LockBody()
    {
        if (locked || body == null || body.isKinematic)
            return;

        HoldFrozen();
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
    /// <remarks>
    /// Заморозка и разморозка выполняются всегда, <see cref="scaleVelocityOnChange"/> отключает только пересчёт
    /// скорости: без него тело после разморозки летит с той же скоростью, с какой замерло, как и при прямой смене
    /// масштаба. «Прежний темп до следующего толчка» при нуле не работает: толчки замороженному драйвер
    /// придерживает, и тело летело бы в остановленном времени, пока его не разморозят.
    /// <para>
    /// Масштаб запоминается и у кинематического тела: по нему <see cref="IsFrozen"/>, а
    /// <see cref="AddScaledForce"/> решает, придержать ли толчок до разморозки.
    /// </para>
    /// </remarks>
    public void ApplyScaleChange()
    {
        if (body == null)
            return;

        var scale = RelativeTimeScale;

        if (Mathf.Approximately(scale, appliedScale))
            return;

        bool wasFrozen = appliedScale <= Mathf.Epsilon;
        bool freezes = scale <= Mathf.Epsilon;

        if (body.isKinematic)
        {
            // Скоростью кинематического тела PhysX не двигает. При разморозке снимаем фиксацию — тело могли
            // зафиксировать до того, как оно стало кинематическим, — и забываем придержанное.
            if (wasFrozen && !freezes)
            {
                UnlockBody();
                ForgetHeldVelocity();
            }
        }
        else if (!wasFrozen && freezes)
        {
            // Замирает: скорость запоминаем, иначе после оттаивания тело стояло бы — из нуля делением прежнюю
            // скорость не восстановить. С пересчётом — в обычном темпе, чтобы вернуть её в новом; без пересчёта —
            // как есть: смена масштаба через ноль не должна менять скорость, раз прямая смена её не меняет.
            if (scaleVelocityOnChange)
            {
                frozenVelocity = body.velocity / appliedScale;
                frozenAngularVelocity = body.angularVelocity / appliedScale;
            }
            else
            {
                keptVelocity = body.velocity;
                keptAngularVelocity = body.angularVelocity;
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            LockBody();
        }
        else if (wasFrozen && !freezes)
        {
            // Оттаивает: летящее продолжает полёт, а придержанные толчки идут в новом темпе.
            UnlockBody();
            body.velocity = keptVelocity + frozenVelocity * scale;
            body.angularVelocity = keptAngularVelocity + frozenAngularVelocity * scale;
            ForgetHeldVelocity();
        }
        else if (!wasFrozen && scaleVelocityOnChange)
        {
            // Пересчёт идёт от ранее применённого масштаба, а не от единицы: иначе
            // повторные изменения накапливали бы ошибку.
            var ratio = scale / appliedScale;
            body.velocity *= ratio;
            body.angularVelocity *= ratio;
        }

        appliedScale = scale;
    }

    /// <summary>
    /// Забыть скорости, придержанные на время заморозки.
    /// </summary>
    private void ForgetHeldVelocity()
    {
        frozenVelocity = Vector3.zero;
        frozenAngularVelocity = Vector3.zero;
        keptVelocity = Vector3.zero;
        keptAngularVelocity = Vector3.zero;
    }

    #endregion
}
