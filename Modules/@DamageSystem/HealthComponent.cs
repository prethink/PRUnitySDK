using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EntityBase))]
public partial class HealthComponent : PRMonoBehaviour, IDamageable, IHealthEntity
{
    #region Поля и свойства

    [SerializeField] protected bool isAlive;

    protected Func<bool> overrideIsAlive;

    #endregion

    #region События

    /// <summary>
    /// Событие смерти сущности.
    /// </summary>
    public event Action<IEntity, IEntity> OnEntityDead;

    /// <summary>
    /// Событие воскрешения сущности.
    /// </summary>
    public event Action<IEntity> OnRevive;

    /// <summary>
    /// Событие спавна сущности.
    /// </summary>
    public event Action<Vector3> OnSpawn;

    /// <summary>
    /// Событие изменения scale.
    /// </summary>
    public event Action<Transform> OnScaleChanged;

    /// <summary>
    /// Событие изменения здоровья.
    /// </summary>
    public event Action<HealthChangedEventArgsBase> OnHealthChange;

    /// <summary>
    /// Вызывается после завершения любой попытки нанесения урона.
    /// </summary>
    public event Action<DamageOutcome> OnDamageProcessed;

    /// <summary>
    /// Последний завершённый результат обработки урона этой сущностью.
    /// </summary>
    public DamageOutcome LastDamageOutcome { get; protected set; }

    /// <summary>
    /// Событие изменения здоровья.
    /// </summary>
    //public event Action<IEntity, DamageBase, float, float, bool> OnHealthChange;

    /// <summary>
    /// Событие попадания в коллайдер.
    /// </summary>
    public event Action<IEntity, Collider, IDamageProvider, DamageResult> OnHitCollider;

    /// <summary>
    /// События попадания.
    /// </summary>
    public event Action<IEntity, Vector3, IDamageProvider, DamageResult> OnHitVector;

    /// <summary>
    /// Inspector-события получают те же аргументы после соответствующих C#-событий.
    /// </summary>
    [field: SerializeField, Header("Unity Events")]
    public UnityEvent<IEntity, IEntity> OnEntityDeadUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<IEntity> OnReviveUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<Vector3> OnSpawnUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<Transform> OnScaleChangedUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<HealthChangedEventArgsBase> OnHealthChangeUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<DamageOutcome> OnDamageProcessedUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<IEntity, Collider, IDamageProvider, DamageResult> OnHitColliderUnity { get; private set; } = new();

    [field: SerializeField]
    public UnityEvent<IEntity, Vector3, IDamageProvider, DamageResult> OnHitVectorUnity { get; private set; } = new();

    #endregion

    #region MonoBehavior

    [field: Header("Здоровье")]

    [field: SerializeField] public bool IsBlockDamage { get; protected set; }

    [field: SerializeField] public float MaxHealth { get; protected set; } = 100;

    [field: SerializeField] public float Health { get; protected set; }



    /// <summary>
    /// Стадия хука «здоровье получило стартовые значения».
    /// </summary>
    /// <remarks>
    /// Сигнатура хука — <c>void Имя()</c>. К этому моменту <see cref="Health"/> уже
    /// заполнено, поэтому отсюда сущность достраивают тем, что должно знать её здоровье:
    /// полосой над головой и подобным. Подписчиков может быть сколько угодно, порядок
    /// задаёт <c>Order</c>.
    /// </remarks>
    public const string HealthInitializedStage = "HealthComponentHealthInitialized";

    protected override void Start()
    {
        base.Start();
        InitHealth();
        this.RunMethodHooks(HealthInitializedStage);
    }

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        Entity = GetComponent<EntityBase>();
        GameObject = Entity.EntityGameObject;
    }

    #endregion

    #region IDamagable

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damageProvider)
    {
        return ProcessDamage(attacker, weapon, damageProvider, null, null);
    }

    private DamageResult ProcessDamage(
        IEntity attacker,
        IWeapon weapon,
        IDamageProvider damageProvider,
        Vector3? hitPoint,
        Collider hitCollider)
    {
        if (damageProvider == null)
            return FailAttempt(DamageResult.NotHandled, attacker, weapon, hitPoint, hitCollider);

        // Пауза - не промах: на Miss вешают звук и эффект уклонения, а урон, пришедший
        // во время паузы, просто не обрабатывается.
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return FailAttempt(DamageResult.NotHandled, attacker, weapon, hitPoint, hitCollider);

        var damageHook = new DamageHookEvent(attacker, weapon, Entity, damageProvider, DamageResult.NotHandled);
        var killed = false;
        void Reject(DamageHookEvent context, DamageResult result)
        {
            if (result == DamageResult.Miss)
                InternalMissedDamage();
            else if (result == DamageResult.Blocked)
                InternalBlockDamage();

            context.DamageResult = result;
            context.Outcome = new DamageOutcome(result, null, Health, Health, hitPoint, hitCollider);
            LastDamageOutcome = context.Outcome;
        }

        try
        {
            HookManager.Instance.Publish(damageHook, context =>
            {
                if (!IsAlive() || context.DamageResult == DamageResult.Miss)
                {
                    Reject(context, DamageResult.Miss);
                    return;
                }

                if (IsBlockDamage || !CanTakeDamage() || context.DamageResult == DamageResult.Blocked)
                {
                    Reject(context, DamageResult.Blocked);
                    return;
                }

                var data = context.DamageProvider?.GetDamageData()?.Clone();
                if (data == null || !IsFiniteNonNegative(data.Damage) ||
                    !IsFiniteNonNegative(data.RawDamage) || !IsFiniteNonNegative(data.AbsorbedDamage))
                {
                    Reject(context, DamageResult.NotHandled);
                    return;
                }

                if (data.RawDamage == 0f && data.Damage != 0f)
                    data.RawDamage = data.Damage;

                InternalTakeDamage();
                var before = Health;
                Health = Mathf.Clamp(before - data.Damage, 0f, MaxHealth);
                var result = Health <= 0f ? DamageResult.Killed : DamageResult.Damaged;
                context.DamageResult = result;
                context.Outcome = new DamageOutcome(result, data, before, Health, hitPoint, hitCollider);
                LastDamageOutcome = context.Outcome;

                if (result == DamageResult.Killed)
                {
                    // Фиксируем смерть до уведомления подписчиков, сохраняя возможность переопределить IsKill.
                    var previousDeferral = deferDeathNotifications;
                    deferDeathNotifications = true;
                    try { killed = IsKill(attacker); }
                    finally { deferDeathNotifications = previousDeferral; }
                }
            }, context =>
            {
                // Сам по себе Supercede отменяет урон: здоровье не должно измениться.
                var result = context.DamageResult == DamageResult.Miss
                    ? DamageResult.Miss
                    : context.DamageResult == DamageResult.Blocked
                        ? DamageResult.Blocked
                        : DamageResult.NotHandled;
                Reject(context, result);
            });
        }
        catch (Exception exception)
        {
            // Ошибка хука не должна отменять уведомления об уже применённом уроне.
            Debug.LogException(exception, this);
        }

        var outcome = damageHook.Outcome;
        if (outcome == null)
            return FailAttempt(DamageResult.NotHandled, attacker, weapon, hitPoint, hitCollider);

        if (outcome.Result == DamageResult.Damaged || outcome.Result == DamageResult.Killed)
        {
            NotifyListeners(OnHealthChange, listener => listener(new HealthChangedEventArgsBase(
                outcome.HealthBefore, outcome.HealthAfter, MaxHealth, outcome)));
            NotifyUnityEvent(() => OnHealthChangeUnity?.Invoke(new HealthChangedEventArgsBase(
                outcome.HealthBefore, outcome.HealthAfter, MaxHealth, outcome)));

            if (killed)
                NotifyDeath(attacker);

            CompleteDamageAttempt(outcome);
            CombatEvents.RaiseOnTakeDamage(new TakeDamageEvent(attacker, Entity, outcome, weapon));
            if (outcome.Result == DamageResult.Killed)
                CombatEvents.RaiseOnKill(new EntityKillEventArgs(attacker, Entity, outcome, weapon));
        }
        else
        {
            CompleteDamageAttempt(outcome);
        }

        RaiseDamageProcessed(attacker, weapon, outcome);
        return outcome.Result;
    }

    private bool deferDeathNotifications;

    private static bool IsFiniteNonNegative(float value)
    {
        return value >= 0f && !float.IsInfinity(value) && !float.IsNaN(value);
    }

    private void NotifyListeners<T>(T listeners, Action<T> invoke) where T : Delegate
    {
        if (listeners == null)
            return;

        foreach (T listener in listeners.GetInvocationList())
        {
            try { invoke(listener); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void NotifyDeath(IEntity killer)
    {
        try { DeathHandle(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
        OnEntityDeadInvoke(killer);
    }

    private void NotifyUnityEvent(Action invoke)
    {
        // Ошибка Inspector-обработчика не должна прерывать завершение обработки урона.
        try { invoke(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    protected virtual void InternalTakeDamage()
    {

    }

    protected virtual void InternalBlockDamage()
    {

    }

    protected virtual void InternalMissedDamage()
    {

    }

    /// <summary>
    /// Сохраняет результат попытки и уведомляет локальных подписчиков.
    /// </summary>
    /// <param name="outcome">Завершённый результат обработки.</param>
    protected virtual void CompleteDamageAttempt(DamageOutcome outcome)
    {
        LastDamageOutcome = outcome;
        NotifyListeners(OnDamageProcessed, listener => listener(outcome));
        NotifyUnityEvent(() => OnDamageProcessedUnity?.Invoke(outcome));
    }

    private void RaiseDamageProcessed(IEntity attacker, IWeapon weapon, DamageOutcome outcome)
    {
        CombatEvents.RaiseOnDamageProcessed(new DamageProcessedEvent(
            attacker,
            this.Entity,
            outcome,
            weapon));
    }

    /// <summary>
    /// Завершает попытку, не изменившую здоровье: промах, блок или необработанный урон.
    /// У всех таких веток одинаковый хвост: снимок исхода, уведомление подписчиков
    /// и публикация события.
    /// </summary>
    /// <param name="result">Причина отказа.</param>
    /// <param name="attacker">Кто наносил урон.</param>
    /// <param name="weapon">Чем наносился урон.</param>
    /// <param name="hitPoint">Точка попадания, если была передана.</param>
    /// <param name="hitCollider">Коллайдер попадания, если был передан.</param>
    /// <returns>Та же причина отказа - для возврата из ProcessDamage.</returns>
    private DamageResult FailAttempt(
        DamageResult result,
        IEntity attacker,
        IWeapon weapon,
        Vector3? hitPoint,
        Collider hitCollider)
    {
        var outcome = new DamageOutcome(result, null, Health, Health, hitPoint, hitCollider);

        CompleteDamageAttempt(outcome);
        RaiseDamageProcessed(attacker, weapon, outcome);

        return result;
    }

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point)
    {
        var result = ProcessDamage(attacker, weapon, damage, point, null);
        if (result != DamageResult.Miss)
        {
            NotifyListeners(OnHitVector, listener => listener(attacker, point, damage, result));
            NotifyUnityEvent(() => OnHitVectorUnity?.Invoke(attacker, point, damage, result));
        }

        return result;
    }

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider)
    {
        var result = ProcessDamage(attacker, weapon, damage, null, collider);
        if (result != DamageResult.Miss)
        {
            NotifyListeners(OnHitCollider, listener => listener(attacker, collider, damage, result));
            NotifyUnityEvent(() => OnHitColliderUnity?.Invoke(attacker, collider, damage, result));
        }

        return result;
    }

    #endregion

    #region Методы

    public IEntity Killer { get; protected set; }

    public EntityBase Entity { get; protected set; }

    public GameObject GameObject { get; protected set; }

    /// <summary>
    /// Инициализация жизней.
    /// </summary>
    /// <remarks>
    /// Подписчиков уведомляем: этим же методом сущность возвращают на сцену после смерти,
    /// а подписки остались с прошлой жизни — без события полоса над головой покажет ноль.
    /// </remarks>
    /// <exception cref="ArgumentException"></exception>
    public virtual void InitHealth()
    {
        if (!IsFiniteNonNegative(MaxHealth) || MaxHealth <= 0)
            throw new ArgumentException("Максимальное здоровье должно быть больше 0!");

        var previousHealth = Health;
        Health = MaxHealth;
        isAlive = Health > 0;

        // Помечаем стартовым: разница здесь положительная и от лечения неотличима,
        // а лечение поднимает полосу над головой. Без метки она выскакивала бы над каждой
        // появившейся сущностью и над каждой возвращённой на сцену.
        var change = new HealthChangedEventArgsBase(previousHealth, Health, MaxHealth, null, isInitial: true);
        NotifyListeners(OnHealthChange, listener => listener(change));
        NotifyUnityEvent(() => OnHealthChangeUnity?.Invoke(change));
    }

    /// <summary>
    /// Убить сущность.
    /// </summary>
    /// <param name="killer">Убийца.</param>
    /// <returns>True - удачно, false нет.</returns>
    public virtual bool IsKill(IEntity killer)
    {
        if (!IsAlive())
            return false;

        isAlive = false;
        Health = 0;
        Killer = killer;
        if (!deferDeathNotifications)
            NotifyDeath(killer);
        return true;
    }

    protected virtual void DeathHandle()
    {

    }

    /// <summary>
    /// Убить сущность.
    /// </summary>
    /// <returns>True - удачно, false нет.</returns>
    public virtual bool Kill()
    {
        return IsKill(GameEventEntityFactory.CreateEventGame());
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <remarks>
    /// Оживление на месте: место спрашиваем у сущности, а не у её корня — у вложенной
    /// сущности это разные точки, и она переехала бы к корню.
    /// </remarks>
    public virtual void Revive()
    {
        Revive(GameEventEntityFactory.CreateEventGame(), MaxHealth, Entity.Position, Entity.Rotation);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="transform">transform.</param>
    public virtual void Revive(Transform transform)
    {
        Revive(GameEventEntityFactory.CreateEventGame(), MaxHealth, transform);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="position">Позиция.</param>
    public virtual void Revive(Vector3 position)
    {
        Revive(GameEventEntityFactory.CreateEventGame(), MaxHealth, position, Quaternion.identity);
    }


    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    public virtual void Revive(float health)
    {
        Revive(GameEventEntityFactory.CreateEventGame(), health, Entity.Position, Entity.Rotation);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="transform">transform.</param>
    public virtual void Revive(float health, Transform transform)
    {
        Revive(GameEventEntityFactory.CreateEventGame(), health, transform);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="position">Позиция.</param>
    public virtual void Revive(float health, Vector3 position)
    {
        Revive(GameEventEntityFactory.CreateEventGame(), health, position, Quaternion.identity);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="reviver">Кто оживляет.</param>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="transform">transform.</param>
    public virtual void Revive(IEntity reviver, float health, Transform transform)
    {
        Revive(reviver, health, transform.position, transform.rotation);
    }

    /// <summary>
    /// Оживить entity.
    /// </summary>
    /// <param name="reviver">Кто оживляет.</param>
    /// <param name="health">Количество жизней при оживление.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="rotation">Поворот.</param>
    public virtual void Revive(IEntity reviver, float health, Vector3 position, Quaternion rotation)
    {
        if (IsAlive())
            return;

        Entity.SetPositionAndRotation(position, rotation);

        isAlive = true;
        Killer = null;
        Health = Mathf.Clamp(health, 1, MaxHealth);
        NotifyListeners(OnRevive, listener => listener(reviver));
        NotifyUnityEvent(() => OnReviveUnity?.Invoke(reviver));
    }

    /// <summary>
    /// Суицид.
    /// </summary>
    /// <returns>True - удачно, false нет.</returns>
    public virtual bool Suicide()
    {
        return IsKill(GameEventEntityFactory.CreateEventSuicide());
    }

    public virtual void Spawn(Vector3 spawnPosition)
    {
        OnSpawnInvoke(spawnPosition);
    }

    /// <summary>
    /// Вызов события смерти сущности.
    /// </summary>
    /// <param name="attacker">Атакующий.</param>
    protected virtual void OnEntityDeadInvoke(IEntity attacker)
    {
        NotifyListeners(OnEntityDead, listener => listener(attacker, Entity));
        NotifyUnityEvent(() => OnEntityDeadUnity?.Invoke(attacker, Entity));
    }

    /// <summary>
    /// Вызвать события спавна.
    /// </summary>
    /// <param name="position">Позиция.</param>
    protected virtual void OnSpawnInvoke(Vector3 position)
    {
        NotifyListeners(OnSpawn, listener => listener(position));
        NotifyUnityEvent(() => OnSpawnUnity?.Invoke(position));
    }

    public bool AddHealth(int health)
    {
        if (health <= 0)
            return false;

        if (!IsAlive())
            return false;

        if (Health >= MaxHealth)
            return false;

        var previousHealth = Health;
        var updateHealth = Math.Clamp(Health + health, Health, MaxHealth);
        Health = updateHealth;
        var change = new HealthChangedEventArgsBase(previousHealth, Health, MaxHealth);
        NotifyListeners(OnHealthChange, listener => listener(change));
        NotifyUnityEvent(() => OnHealthChangeUnity?.Invoke(change));
        return true;
    }

    private void OnHeathChange()
    {

    }

    /// <summary>
    /// Может ли сущность принимать урон.
    /// </summary>
    /// <returns>True - удачно, false нет.</returns>
    public virtual bool CanTakeDamage()
    {
        return true;
    }

    public virtual void InvokeOnScaleChanged()
    {
        NotifyListeners(OnScaleChanged, listener => listener(transform));
        NotifyUnityEvent(() => OnScaleChangedUnity?.Invoke(transform));
    }

    public void SetOverrideIsAlive(Func<bool> overrideFunc)
    {
        overrideIsAlive = overrideFunc;
    }

    public virtual bool IsAlive()
    {
        return overrideIsAlive != null 
            ? overrideIsAlive() 
            : isAlive;
    }

    public virtual void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }

    #endregion
}
