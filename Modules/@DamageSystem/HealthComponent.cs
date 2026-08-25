using System;
using UnityEngine;

[RequireComponent(typeof(EntityBase))]
public class HealthComponent : PRMonoBehaviour, IDamageable, IHealthEntity
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

    #endregion

    #region MonoBehavior

    [field: Header("Здоровье")]
    [field: SerializeField] public bool HideOnDead { get; protected set; } = true;

    [field: SerializeField] public bool IsBlockDamage { get; protected set; }

    [field: SerializeField] public float MaxHealth { get; protected set; } = 100;

    [field: SerializeField] public float Health { get; protected set; }


    protected override void Start()
    {
        base.Start();
        InitHealth();
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

        var damageHook = HookManager.Instance.Publish(new DamageHookEvent(attacker, weapon, this.Entity, damageProvider, DamageResult.NotHandled));
        if (!IsAlive() || damageHook.DamageResult == DamageResult.Miss)
        {
            InternalMissedDamage();
            return FailAttempt(DamageResult.Miss, attacker, weapon, hitPoint, hitCollider);
        }

        if (IsBlockDamage || !CanTakeDamage() || damageHook.DamageResult == DamageResult.Blocked)
        {
            InternalBlockDamage();
            return FailAttempt(DamageResult.Blocked, attacker, weapon, hitPoint, hitCollider);
        }

        if (damageHook.DamageProvider == null)
            return FailAttempt(DamageResult.NotHandled, attacker, weapon, hitPoint, hitCollider);

        InternalTakeDamage();
        var startHealth = Health;
        var currentDamage = damageHook.DamageProvider.GetDamageData();
        if (currentDamage == null)
            return FailAttempt(DamageResult.NotHandled, attacker, weapon, hitPoint, hitCollider);

        if (currentDamage.RawDamage == 0f && currentDamage.Damage != 0f)
            currentDamage.RawDamage = currentDamage.Damage;

        var nextHealth = Mathf.Clamp(Health - currentDamage.Damage, 0, MaxHealth);
        Health = nextHealth;
        var result = nextHealth <= 0 ? DamageResult.Killed : DamageResult.Damaged;
        var outcome = new DamageOutcome(
            result,
            currentDamage,
            startHealth,
            nextHealth,
            hitPoint,
            hitCollider);

        OnHealthChange?.Invoke(new HealthChangedEventArgsBase(
            startHealth,
            nextHealth,
            MaxHealth,
            outcome));

        if (nextHealth <= 0)
        {
            IsKill(attacker);
        }

        CompleteDamageAttempt(outcome);
        CombatEvents.RaiseOnTakeDamage(new TakeDamageEvent(attacker, this.Entity, outcome, weapon));

        if (result == DamageResult.Killed)
            CombatEvents.RaiseOnKill(new EntityKillEventArgs(attacker, this.Entity, outcome, weapon));

        RaiseDamageProcessed(attacker, weapon, outcome);

        return result;
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
        OnDamageProcessed?.Invoke(LastDamageOutcome);
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
    /// <para>
    /// Собран в один метод, потому что у всех таких веток одинаковый хвост - снимок
    /// исхода, уведомление подписчиков и публикация события. Раньше он был скопирован
    /// в каждую ветку, и добавление новой причины отказа легко теряло один из шагов.
    /// </para>
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
            OnHitVector?.Invoke(attacker, point, damage, result);

        return result;
    }

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider)
    {
        var result = ProcessDamage(attacker, weapon, damage, null, collider);
        if (result != DamageResult.Miss)
            OnHitCollider?.Invoke(attacker, collider, damage, result);

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
    /// <exception cref="ArgumentException"></exception>
    public virtual void InitHealth()
    {
        if (MaxHealth <= 0)
            throw new ArgumentException("Максимальное здоровье должно быть больше 0!");

        Health = MaxHealth;
        isAlive = Health > 0;
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
        DeathHandle();
        Health = 0;
        Killer = killer;
        ChangeVisibleEntity();
        OnEntityDeadInvoke(killer);
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
    public virtual void Revive()
    {
        Revive(GameEventEntityFactory.CreateEventGame(), MaxHealth, Entity.transform);
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
        Revive(GameEventEntityFactory.CreateEventGame(), health, Entity.transform);
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

        Entity.transform.position = position;
        Entity.transform.rotation = rotation;

        isAlive = true;
        Killer = null;
        Health = Mathf.Clamp(health, 1, MaxHealth);
        ChangeVisibleEntity();
        OnRevive?.Invoke(reviver);
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
    /// Изменить видимость entity.
    /// </summary>
    protected virtual void ChangeVisibleEntity()
    {
        if (HideOnDead)
            Entity.gameObject.SetActive(IsAlive());
    }

    /// <summary>
    /// Вызов события смерти сущности.
    /// </summary>
    /// <param name="attacker">Атакующий.</param>
    protected virtual void OnEntityDeadInvoke(IEntity attacker)
    {
        OnEntityDead?.Invoke(attacker, this.Entity);
    }

    /// <summary>
    /// Вызвать события спавна.
    /// </summary>
    /// <param name="position">Позиция.</param>
    protected virtual void OnSpawnInvoke(Vector3 position)
    {
        OnSpawn?.Invoke(position);
    }

    public bool AddHealth(int health)
    {
        if (!IsAlive())
            return false;

        if (Health >= MaxHealth)
            return false;

        var previousHealth = Health;
        var updateHealth = Math.Clamp(Health + health, Health, MaxHealth);
        Health = updateHealth;
        OnHealthChange?.Invoke(new HealthChangedEventArgsBase(previousHealth, Health, MaxHealth));
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
        OnScaleChanged?.Invoke(transform);
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

    #endregion
}
