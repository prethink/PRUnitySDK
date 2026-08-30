using System;
using UnityEngine;

/// <summary>
/// Сущность с описанием своего типа.
/// </summary>
/// <remarks>
/// Тип-параметр задаёт две вещи сразу: чем описывается сущность и куда положить ссылку.
/// Наследник получает типизированный доступ к своим полям описания, а инспектор знает,
/// ассет какого типа предлагать создать.
/// <para>
/// Ограничение по <see cref="EntityMetadataBase"/>, а не по <c>IEntityMetadata</c>:
/// интерфейсное поле Unity не сериализует, и ссылку негде было бы хранить.
/// </para>
/// </remarks>
/// <typeparam name="TMetadata">Тип описания.</typeparam>
public abstract partial class EntityBase<TMetadata> : EntityBase
    where TMetadata : EntityMetadataBase
{
    /// <summary>
    /// Описание сущности.
    /// </summary>
    [field: SerializeField, Header("Описание")]
    public TMetadata Metadata { get; protected set; }

    /// <inheritdoc />
    /// <remarks>
    /// Имя берётся из описания, поэтому наследнику остаётся объявить только тип сущности.
    /// Заглушка вместо пустой строки нужна, чтобы отсутствие описания было видно
    /// в отладчике, а не выглядело как безымянный объект.
    /// </remarks>
    public override string Name => Metadata != null ? Metadata.GetTranslate() : "NotInitialized";

    /// <inheritdoc />
    /// <remarks>
    /// Вид берётся из описания, поэтому наследнику объявлять нечего - а <see cref="Entity"/>
    /// и вовсе обходится без наследника. Переопределить всё равно можно: сущностям
    /// с определением описание достаётся от позиции, и вид они задают сами.
    /// <para>
    /// Незаполненное описание даёт <c>Unknown</c>, а не <c>null</c>: по виду ведёт учёт
    /// трекер, и пустой ключ уронил бы его на регистрации.
    /// </para>
    /// </remarks>
    public override Enumeration EntityType =>
        Metadata != null && Metadata.EntityType != null
            ? Metadata.EntityType.ToEnumeration() ?? EntityTypeEnumerationProvider.Unknown
            : EntityTypeEnumerationProvider.Unknown;

    /// <inheritdoc />
    /// <remarks>
    /// Поверх описания ложится <see cref="IEntityMetadataProvider"/> с того же объекта,
    /// если он есть: так отдельный экземпляр получает своё имя или иконку, не заводя
    /// собственного ассета.
    /// </remarks>
    protected override void InitializeEntityMetadata()
    {
        Description = new EntityDescription(Metadata, this.GetComponent<IEntityMetadataProvider>()?.EntityMetadata);
    }
}


/// <summary>
/// Базовый класс сущности.
/// </summary>
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

    public abstract Enumeration EntityType { get; }

    public abstract string Name { get; }

    public virtual bool OnScene => this.EntityGameObject.activeSelf;

    public virtual GameObject EntityGameObject => entityGameObject != null ? entityGameObject : gameObject;
    public virtual GameObject RootEntityObject => rootGameObject != null ? rootGameObject : gameObject;

    public virtual void GenerateId(Func<long> register)
    {
        Id = register();
    }

    public virtual void DestroyEntity()
    {
        DestroyEntity(new EntityDestroyOptions());
    }

    public virtual void DestroyEntity(EntityDestroyOptions options)
    {
        if (!options.FullDestroy && InPool)
            return;

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
        else if (EntityDisposeAction == EntityDisposeAction.HideInPool && !InPool && PoolBehaviour.IsInitialize)
        {
            OnDestroyPool();
            return;
        }
        else if(EntityDisposeAction == EntityDisposeAction.HideInPool && InPool || !InPool && !PoolBehaviour.IsInitialize)
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

    protected virtual void RegisterEntity()
    {
        PRUnitySDK.Trackers.Entities.Register(this);
    }

    protected virtual void UnregisterEntity()
    {
        PRUnitySDK.Trackers.Entities.Unregister(this);
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

        InitializeEntityMetadata();
        InitializeEntity();
    }

    #endregion

    #region MonoBehaviour


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

    protected override void Start()
    {
        RegisterEntity();
        base.Start();
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        UnregisterEntity();
        base.UnRegisterEventsOnDestroy();
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

    public EntityDescription Description { get; protected set; }

    protected abstract void InitializeEntityMetadata();

    public virtual Enumeration GetTimeScaleLayer()
    {
        return PRTimeScaleEnumerationProvider.Global;
    }

    #endregion
}


public enum EntityDisposeAction
{
    HideInPool,
    Destroy,
}
