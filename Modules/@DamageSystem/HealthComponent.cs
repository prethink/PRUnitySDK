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

    #endregion

    #region IDamagable

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damageProvider)
    {
        var damageHook = HookManager.Instance.Publish(new DamageHookEvent(attacker, weapon, this.Entity, damageProvider, DamageResult.NotHandled));
        if (!IsAlive() || PRUnitySDK.PauseManager.IsLogicPaused || damageHook.DamageResult == DamageResult.Miss)
        {
            InternalMissedDamage();
            return DamageResult.Miss;
        }

        if (IsBlockDamage || !CanTakeDamage() || damageHook.DamageResult == DamageResult.Blocked)
        {
            InternalBlockDamage();
            return DamageResult.Blocked;
        }

        InternalTakeDamage();
        var startHealth = Health;
        var currentDamage = damageHook.DamageProvider.GetDamageData();
        var nextHealth = Mathf.Clamp(Health - currentDamage.Damage, 0, MaxHealth);
        var damageInflicted = startHealth - Health;
        OnHealthChange?.Invoke(new HealthChangedEventArgsBase(nextHealth, MaxHealth));

        if (nextHealth <= 0)
        {
            //if (Kill(attacker))
            //    EventBus.RaiseEvent<IEntityDeathEvents>(x => x.OnDeath(attacker, weapon, this));

            return DamageResult.Killed;
        }
        else
        {
            Health = nextHealth;
        }
        //EventBus.RaiseEvent<IEntityTakeDamageEvents>(x => x.OnTakeDamage(attacker, weapon, this, damageHook.DamageProvider));
        return DamageResult.Damaged;
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

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point)
    {
        var result = TakeDamage(attacker, weapon, damage);
        if (result != DamageResult.Miss)
            OnHitVector?.Invoke(attacker, point, damage, result);

        return result;
    }

    public DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider)
    {
        var result = TakeDamage(attacker, weapon, damage);
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
    public virtual bool Kill(IEntity killer)
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
        return Kill(GameEventEntityFactory.CreateEventGame());
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
        return Kill(GameEventEntityFactory.CreateEventSuicide());
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

        var updateHealth = Math.Clamp(Health + health, Health, MaxHealth);
        Health = updateHealth;
        //OnHealthChange?.Invoke()
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
