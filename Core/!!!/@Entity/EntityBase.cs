using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Базовый класс сущности.
/// </summary>
public abstract partial class EntityBase : PRMonoBehaviour, IEntity, IPoolable
{
    #region Поля и свойства

    [Header("Базовая сущность")]
    /// <summary>
    /// Действиет при уничтожение.
    /// </summary>
    [SerializeField] protected EntityDisposeAction EntityDisposeAction;

    /// <summary>
    /// Переведенное имя сущности..
    /// </summary>
    [SerializeField] protected Translator entityTranslateName;

    /// <summary>
    /// Спрайт иконки сущности.
    /// </summary>
    [SerializeField] protected Sprite entityIcon;

    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    [SerializeField] protected GameObject entityGameObject;

    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    [SerializeField] protected GameObject rootGameObject;

    /// <summary>
    /// Игровой объект сущности.
    /// </summary>
    [SerializeField] protected Animator animator;

    /// <summary>
    /// Время жизни сущности.
    /// </summary>
    [field:SerializeField] public EntityLifeTime LifeTime { get; protected set; }

    /// <summary>
    /// Качество сущности.
    /// </summary>
    [field: SerializeField] public QualityType Quality { get; private set; }

    #endregion

    #region IEntity

    public event Action<EntityBase> OnEntityDestroy;
    
    public long Id { get; protected set; }

    public abstract Type EntityType { get; }

    public abstract string Name { get; }

    public virtual bool OnScene => this.gameObject.activeSelf;

    public virtual Sprite Icon => !entityIcon.IsUnityNull() ? entityIcon : PRUnitySDK.Database.Sprites.Entities.EntityBase;

    public virtual GameObject EntityGameObject => entityGameObject != null ? entityGameObject : gameObject;
    public virtual GameObject RootEntityObject => rootGameObject != null ? rootGameObject : gameObject;

    [field: SerializeField] public Sprite KillIcon { get; protected set; }

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

    public virtual long GetCurrentLevel()
    {
        return 1;
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

    #endregion

    #region MonoBehaviour

    protected virtual void OnDestroy()
    {
        PRUnitySDK.Trackers.Entities.Unregister(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
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
        if(RootEntityObject.TryGetComponent<Rigidbody>(out var rb))
            RegisterRigidBody(rb);

        base.Start();
    }

    #endregion

    #region IPollable 

    public PoolBehaviour PoolBehaviour { get; private set; }

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

    #endregion

    #region IGameSessionListener

    public IEntityInfo Info => throw new NotImplementedException();

    protected virtual void EntityInitialize()
    {

    }

    #endregion
}


public enum EntityDisposeAction
{
    HideInPool,
    Destroy,
}