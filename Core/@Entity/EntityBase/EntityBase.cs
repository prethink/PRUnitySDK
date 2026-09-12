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
    public override Enumeration EntityType => Metadata?.EntityType?.ToEnumeration() ?? EntityTypeEnumerations.Unknown;

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
public abstract partial class EntityBase : PRMonoBehaviour, IEntity, IPoolable, IRestoreHideEntitiesEvent
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
    [SerializeField] protected EnumerationReference<EntityDisposeEnumerations> EntityDisposeAction = new();

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

    public virtual bool OnScene => this.EntityGameObject.activeInHierarchy;

    public virtual GameObject EntityGameObject => entityGameObject != null ? entityGameObject : gameObject;
    public virtual GameObject RootEntityObject => rootGameObject != null ? rootGameObject : gameObject;

    /// <summary>
    /// Где сущность находится на самом деле.
    /// </summary>
    /// <remarks>
    /// Позиция <see cref="EntityGameObject"/>, а не корня иерархии: сущностью бывает вложенный
    /// объект со своим смещением, и корень стоит в стороне от того, что видит игрок.
    /// Когда сущность и есть корень, разницы нет.
    /// </remarks>
    public Vector3 Position => EntityGameObject.transform.position;

    /// <summary>
    /// Куда сущность повёрнута на самом деле.
    /// </summary>
    public Quaternion Rotation => EntityGameObject.transform.rotation;

    /// <summary>
    /// Ставит сущность в точку.
    /// </summary>
    /// <remarks>
    /// В точке оказывается <see cref="EntityGameObject"/>, а переносится вся иерархия целиком:
    /// поставить в точку корень — значит промахнуться на величину смещения вложенного объекта,
    /// а подвинуть только вложенный объект — оторвать его от собственной иерархии.
    /// </remarks>
    /// <param name="position">Куда поставить сущность.</param>
    public virtual void SetPosition(Vector3 position)
    {
        RootEntityObject.transform.position += position - Position;
    }

    /// <summary>
    /// Ставит сущность в точку и разворачивает.
    /// </summary>
    /// <param name="position">Куда поставить сущность.</param>
    /// <param name="rotation">Каким поворотом поставить сущность.</param>
    public virtual void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        var root = RootEntityObject.transform;

        // Сначала поворот: он уводит вложенный объект по дуге вокруг корня, поэтому
        // смещение до сущности считается уже после разворота.
        root.rotation = rotation * Quaternion.Inverse(Quaternion.Inverse(root.rotation) * Rotation);
        root.position += position - Position;
    }

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
            OnDestroyPool(true);
            Destroy(this.gameObject);
            return;
        }

        if (EntityDisposeAction.ToEnumeration() == EntityDisposeEnumerations.Destroy)
        {
            OnDestroyPool(true);
            Destroy(this.gameObject);
            return;
        }
        // Условия на activeSelf нет: у спрятанной сущности иначе не остаётся подходящей
        // ветки, и она доходит до предупреждения про пул с полным уничтожением.
        else if (EntityDisposeAction.ToEnumeration() == EntityDisposeEnumerations.Hide)
        {
            EntityGameObject.SetActive(false);
            return;
        }
        else if (EntityDisposeAction.ToEnumeration() == EntityDisposeEnumerations.HideInPool && !InPool && PoolBehaviour.IsInitialize)
        {
            OnDestroyPool();
            return;
        }
        else if(EntityDisposeAction.ToEnumeration() == EntityDisposeEnumerations.HideInPool && InPool || !InPool && !PoolBehaviour.IsInitialize)
        {
            PRLog.WriteWarning(this, $"Entity {EntityType} - {Name} использует настройку {nameof(EntityDisposeEnumerations.HideInPool)}, но при этом создается не через pool system. Объект полностью уничтожен.");
            Destroy(this.gameObject);
            return;
        }

        throw new NotImplementedException();
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
        ApplyDescriptionOverrides();
        InitializeEntity();
    }

    /// <summary>
    /// Отдаёт описание компонентам, которые переопределяют его части.
    /// </summary>
    /// <remarks>
    /// Собирает их сущность, а не компоненты записываются сами: порядок <c>Awake</c> между
    /// компонентами одного объекта не задан, и переопределение пришло бы раньше, чем
    /// описание вообще создано.
    /// <para>
    /// Ищем только на своём объекте: у вложенных сущностей свои описания, и переопределение
    /// из глубины иерархии досталось бы чужому.
    /// </para>
    /// </remarks>
    protected virtual void ApplyDescriptionOverrides()
    {
        if (Description == null)
            return;

        foreach (IEntityDescriptionOverride descriptionOverride in GetComponents<IEntityDescriptionOverride>())
            descriptionOverride.Apply(Description);
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

    [field: SerializeField] public EntityDescription Description { get; protected set; }

    protected abstract void InitializeEntityMetadata();

    public virtual Enumeration GetTimeScaleLayer()
    {
        return PRTimeScaleEnumerations.Global;
    }

    /// <summary>
    /// Возвращает на сцену сущность, спрятанную при уничтожении.
    /// </summary>
    /// <remarks>
    /// Инициализация повторяется, как при выдаче из пула: для сущности это новое появление
    /// на сцене, и состояние прошлой жизни на ней остаться не должно. Объект включается
    /// до инициализации, иначе она не сможет запустить корутины и работать с компонентами.
    /// </remarks>
    public virtual void RestoreHideEvent(RestoreHideEntitiesEventArgs e)
    {
        if (EntityDisposeAction.ToEnumeration() != EntityDisposeEnumerations.Hide || EntityGameObject.activeSelf)
            return;

        EntityGameObject.SetActive(true);
        InitializeEntity();
    }

    #endregion
}