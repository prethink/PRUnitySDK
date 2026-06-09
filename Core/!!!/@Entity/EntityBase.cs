using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Базовый класс сущности.
/// </summary>
[RequireComponent(typeof(RigidBodyPauseMonitor), typeof(AnimatorPauseMonitor))]
public abstract partial class EntityBase : PRMonoBehaviour, IEntity, IPoolable
{
    #region Поля и свойства

    [Header("Ссылки")]
    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    [SerializeField] protected GameObject entityGameObject;

    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    [SerializeField] protected GameObject rootGameObject;

    [Header("Жизненный цикл")]
    /// <summary>
    /// Действие при уничтожение.
    /// </summary>
    [SerializeField] protected EntityDisposeAction EntityDisposeAction;

    /// <summary>
    /// Время жизни сущности.
    /// </summary>
    [field:SerializeField] public EntityLifeTime LifeTime { get; protected set; }

    #endregion

    #region IEntity

    public event Action<EntityBase> OnEntityDestroy;
    
    public long Id { get; protected set; }

    public abstract Type EntityType { get; }

    public abstract string Name { get; }

    public virtual bool OnScene => this.EntityGameObject.activeSelf;

    public virtual GameObject EntityGameObject => entityGameObject != null ? entityGameObject : gameObject;
    public virtual GameObject RootEntityObject => rootGameObject != null ? rootGameObject : gameObject;

    protected RigidBodyPauseMonitor rigidBodyPauseMonitor;

    protected AnimatorPauseMonitor animatorPauseMonitor;

    public virtual void GenerateId(Func<int> register)
    {
        Id = register();
    }

    public virtual void DestroyEntity()
    {
        DestroyEntity(new EntityDestroyOptions());
    }

    public virtual void DestroyEntity(EntityDestroyOptions options)
    {
        OnEntityDestroy?.Invoke(this);

        if(options.FullDestroy)
        {
            Destroy(this.gameObject);
            return;
        }

        if (EntityDisposeAction == EntityDisposeAction.Destroy)
        {
            OnDestroyPool(true);
            Destroy(this.gameObject);
            return;
        }
        else if (EntityDisposeAction == EntityDisposeAction.HideInPool && !InPool)
        {
            OnDestroyPool();
            return;
        }
        else if(EntityDisposeAction == EntityDisposeAction.HideInPool && InPool)
        {
            PRLog.WriteWarning(this, $"Entity {EntityType} - {Name} использует настройку {nameof(EntityDisposeAction.HideInPool)}, но при этом создается не через pool system. Объект полностью уничтожен.");
            Destroy(this.gameObject);
            return;
        }

        throw new NotImplementedException();
    }

    public virtual DamageData GetDamageData()
    {
        return CreateBaseDamageData();
    }

    public virtual DamageData CreateBaseDamageData()
    {
        return new DamageData()
        {
            Damage = 1
        };
    }

    #endregion

    #region Методы

    public void SetGameObjectEntity(GameObject entity)
    {
        entityGameObject = entity;
    }

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        InitializeEntityInfo();
        InitializeEntity();

        rigidBodyPauseMonitor = GetComponent<RigidBodyPauseMonitor>();
        animatorPauseMonitor = GetComponent<AnimatorPauseMonitor>();
    }

    #endregion

    #region MonoBehaviour

    protected virtual void OnDestroy()
    {
        PRUnitySDK.Trackers.Entities.Unregister(this);
    }

    protected override void OnEnable()
    {
        PoolBehaviour.OnInitializeObject += InitializeFromPool;
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        PoolBehaviour.OnInitializeObject -= InitializeFromPool;
        base.OnDisable();
    }

    #endregion

    #region Методы

    public virtual void RegisterEntity()
    {
        PRUnitySDK.Trackers.Entities.Register(this);
    }

    protected override void Start()
    {
        RegisterEntity();
        base.Start();
    }

    #endregion

    #region IPollable 

    public PoolBehaviour PoolBehaviour { get; private set; } = new();

    public bool InPool => PoolBehaviour.InPool;

    public virtual void RegisterPoolObject(PoolObject poolObject)
    {
        PoolBehaviour.RegisterPoolObject(poolObject);
    }

    public virtual void InitializationPoolObject()
    {
        PoolBehaviour.InitializationPoolObject();
    }

    public virtual void OnDestroyPool(bool fullDestroy = false)
    {
        PoolBehaviour.OnDestroyPool(fullDestroy);
    }

    public virtual string GetPoolKey()
    {
        return EntityType.ToString();
    }

    protected void InitializeFromPool(bool isFirstPool)
    {
        if (isFirstPool)
            return;

        InitializeEntity();
    }

    protected virtual void InitializeEntity()
    {

    }

    #endregion

    #region IGameSessionListener

    public EntityInfoContainer Info { get; protected set; }

    [SerializeField, Header("Информация о типе сущностей")] protected EntityInfoBase entityInfoData;
    protected IEntityInfo baseEntityInfo;
    protected IEntityInfo overrideEntityInfo;

    protected virtual void EntityInitializeBaseEntityInfo()
    {
        baseEntityInfo = entityInfoData;
        baseEntityInfo ??= PRUnitySDK.Database.EntityInfo.Data.First();
    }

    protected virtual void EntityInitializeOverrideEntityInfo()
    {
        overrideEntityInfo = GetComponent<IEntityInfoProvider>()?.EntityInfo;
    }

    protected virtual void InitializeEntityInfo()
    {
        InitializeDefaultEntityInfo();
        EntityInitializeOverrideEntityInfo();

        if (baseEntityInfo != null && overrideEntityInfo != null)
        {
            Info = new EntityInfoContainer(baseEntityInfo, overrideEntityInfo);
        }
        else if (baseEntityInfo != null)
        {
            Info = new EntityInfoContainer(baseEntityInfo);
        }
        else if (overrideEntityInfo != null)
        {
            Info = new EntityInfoContainer(overrideEntityInfo);
        }
        else
            throw new InvalidOperationException("Not have entity info");

        PostEntityInitialize();
    }

    protected virtual void PostEntityInitialize()
    {

    }

    protected virtual void InitializeDefaultEntityInfo()
    {
        EntityInitializeBaseEntityInfo(); 
    }

    #endregion
}


public enum EntityDisposeAction
{
    HideInPool,
    Destroy,
}