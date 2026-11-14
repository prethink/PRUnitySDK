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

    [Header("Время")]
    [Tooltip("Слои времени: какие замедления задевают сущность. Можно несколько — враг: NPC и Enemy. " +
             "Только World — сущность идёт в темпе мира.")]
    [SerializeField] protected TimeLayer timeLayers = TimeLayer.World;

    /// <summary>
    /// Место, поворот и размер, с которыми сущность появилась; к ним её возвращает
    /// восстановление.
    /// </summary>
    private Vector3 startPosition;

    private Quaternion startRotation;

    private Vector3 startScale;

    private bool startTransformCaptured;

    private EntityWireVisual wireVisual;

    /// <summary>
    /// Идёт событие уничтожения: состояние уже сменилось, а объект ещё не спрятан и не уничтожен.
    /// </summary>
    private bool endingLife;

    #endregion

    #region IEntity

    /// <summary>
    /// Жизнь сущности закончилась. Подписка действует весь срок компонента.
    /// </summary>
    /// <remarks>
    /// Возврат в пул подписчиков не снимает: сущность из пула приходит снова, и тот, кто подписался один раз
    /// на экземпляр (спавнер, раздача оружия), получает событие в конце каждой её жизни. Подписку на одну
    /// жизнь даёт <see cref="OnLifeEnded"/>.
    /// <para>
    /// Приходит один раз на жизнь и только когда жизнь закончила игра: <c>DestroyEntity</c> или возврат в пул.
    /// Объект, уничтоженный Unity (выгрузка сцены, очистка пула), события не поднимает.
    /// </para>
    /// </remarks>
    public event Action<EntityBase> OnEntityDestroy;

    /// <summary>
    /// Текущая жизнь сущности закончилась. Подписка действует одну жизнь.
    /// </summary>
    /// <remarks>
    /// После вызова список подписчиков пуст: следующая выдача из пула или возврат спрятанной прежних
    /// подписчиков не помнит. Для того, что привязано к одной жизни, — привязка к сессии, занятое место,
    /// владелец предмета. Отписываться самому не нужно; подписка, сделанная из обработчика, относится
    /// уже к следующей жизни. Приходит перед <see cref="OnEntityDestroy"/>, у игрока тоже.
    /// Уничтожение объекта Unity подписчиков снимает без вызова.
    /// </remarks>
    public event Action<EntityBase> OnLifeEnded;

    public long Id { get; protected set; }

    /// <summary>
    /// Что сейчас с логической жизнью сущности.
    /// </summary>
    /// <remarks>
    /// Состояние хранится, а не выводится из активности объекта и настройки: выключить объект может кто угодно,
    /// а уничтожение Unity откладывает до конца кадра.
    /// </remarks>
    public EntityLifeState LifeState { get; private set; } = EntityLifeState.Created;

    /// <summary>
    /// Жизнь сущности закончилась: она спрятана, лежит в пуле или уничтожена.
    /// </summary>
    public bool IsLifeOver => LifeState is EntityLifeState.Hidden or EntityLifeState.Pooled or EntityLifeState.Destroyed;

    public abstract Enumeration EntityType { get; }

    public abstract string Name { get; }

    public virtual bool OnScene => this.EntityGameObject.activeInHierarchy;

    /// <summary>
    /// Настроенное действие при уничтожении сущности.
    /// </summary>
    public Enumeration DisposeAction => EntityDisposeAction.ToEnumeration();

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
    /// <para>
    /// Физическому телу сущности поза ставится отдельно — см. <see cref="SyncBody"/>.
    /// </para>
    /// </remarks>
    /// <param name="position">Куда поставить сущность.</param>
    public virtual void SetPosition(Vector3 position)
    {
        RootEntityObject.transform.position += position - Position;

        SyncBody(withRotation: false);
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

        SyncBody(withRotation: true);
    }

    /// <summary>
    /// Ставит телу на <see cref="EntityGameObject"/> ту же позу, что у его transform.
    /// </summary>
    /// <remarks>
    /// Без этого перенос вне шага физики у интерполируемого тела пропадал через раз:
    /// интерполяция каждый кадр пишет в transform позу тела и перетирает перенос, если
    /// шаг физики не успел пройти до следующего кадра. На время рекламы YG2 ставит
    /// <c>Time.timeScale = 0</c>, шагов нет вовсе — телепорт после ролика не срабатывал.
    /// Перенос из триггера идёт внутри шага и этого не замечал.
    /// <para>
    /// Только тело самой сущности: остальное в её иерархии — кирки на орбите, кости ragdoll —
    /// едет за корнем само. И без <c>Physics.SyncTransforms</c>: поза тела уходит в физику
    /// сразу, а общая синхронизация стоила бы дорого там, где сущность ставят каждый кадр,
    /// как повтор призрака.
    /// </para>
    /// </remarks>
    /// <param name="withRotation">Ставить ли и поворот.</param>
    protected void SyncBody(bool withRotation)
    {
        if (EntityGameObject == null || !EntityGameObject.TryGetComponent(out Rigidbody body))
            return;

        Transform bodyTransform = body.transform;

        body.position = bodyTransform.position;

        if (withRotation)
            body.rotation = bodyTransform.rotation;
    }

    public virtual void GenerateId(Func<long> register)
    {
        Id = register();
    }

    public virtual void DestroyEntity()
    {
        DestroyEntity(new EntityDestroyOptions());
    }

    /// <summary>
    /// Заканчивает жизнь сущности: прячет, возвращает в пул или уничтожает — по её настройке.
    /// </summary>
    /// <remarks>
    /// Повторный вызов безопасен: жизнь заканчивается один раз, и <see cref="OnEntityDestroy"/> приходит один раз —
    /// даже если второй вызов пришёл из обработчика события или до конца кадра, пока Unity ещё не уничтожила
    /// объект. Состояние меняется до события именно поэтому.
    /// <para>
    /// Полное уничтожение сущности, чья жизнь уже закончилась (спрятана, лежит в пуле), убирает объект
    /// без нового события: умирать второй раз нечему.
    /// </para>
    /// </remarks>
    public virtual void DestroyEntity(EntityDestroyOptions options)
    {
        bool fullDestroy = options != null && options.FullDestroy;

        AcceptReturnByHand();

        if (IsLifeOver)
        {
            if (fullDestroy && LifeState != EntityLifeState.Destroyed)
            {
                LifeState = EntityLifeState.Destroyed;
                DestroyObject();
            }

            return;
        }

        EntityLifeState end = ResolveEndState(fullDestroy);

        TryEndLife(end);
        RaiseEntityDestroyed();

        // Обработчик мог довести дело до другого конца (уничтожить спрятанную целиком): тогда он уже всё сделал.
        if (LifeState != end)
            return;

        if (end == EntityLifeState.Destroyed)
            DestroyObject();
        else if (end == EntityLifeState.Pooled)
            OnDestroyPool();
        else
            HideObject();
    }

    /// <summary>
    /// Спрятанную сущность включили напрямую, мимо <c>RestoreHideEntities</c>: для игры она снова на сцене.
    /// </summary>
    /// <remarks>
    /// Так делает существующий код игр (вещь кладут в контейнер и включают её объект). Без этого шага
    /// такая сущность считалась бы спрятанной и повторное уничтожение молча не срабатывало бы.
    /// Пока идёт событие уничтожения, объект ещё включён — это не возврат.
    /// </remarks>
    private void AcceptReturnByHand()
    {
        if (!endingLife && LifeState == EntityLifeState.Hidden && EntityGameObject.activeSelf)
            LifeState = EntityLifeState.Active;
    }

    /// <summary>
    /// Заканчивает жизнь, если она ещё идёт.
    /// </summary>
    /// <remarks>
    /// Для наследника, который уничтожает сущность своим путём (игрок убирает корень и о выходе сообщает
    /// своим событием): так его путь тоже становится безопасным при повторном вызове.
    /// </remarks>
    /// <param name="end">Чем жизнь закончилась: спрятана, в пуле или уничтожена.</param>
    /// <returns><c>false</c>, если жизнь уже закончилась раньше.</returns>
    protected bool TryEndLife(EntityLifeState end)
    {
        if (IsLifeOver)
            return false;

        LifeState = end;

        // Список забирается до вызова: подписка из обработчика остаётся на следующую жизнь.
        Action<EntityBase> lifeEnded = OnLifeEnded;
        OnLifeEnded = null;
        Notify(lifeEnded);

        return true;
    }

    /// <summary>
    /// Сообщает о конце жизни. Зовётся один раз на жизнь, сразу после смены состояния.
    /// </summary>
    private void RaiseEntityDestroyed()
    {
        Notify(OnEntityDestroy);
    }

    /// <summary>
    /// Зовёт обработчиков конца жизни, пометив, что объект ещё не спрятан и не уничтожен.
    /// </summary>
    private void Notify(Action<EntityBase> handlers)
    {
        if (handlers == null)
            return;

        bool wasEnding = endingLife;
        endingLife = true;

        try
        {
            handlers.Invoke(this);
        }
        finally
        {
            endingLife = wasEnding;
        }
    }

    /// <summary>
    /// Чем закончится жизнь при нынешней настройке сущности.
    /// </summary>
    /// <remarks>
    /// Решается до события уничтожения: о негодной настройке сообщаем, пока сущность ещё цела,
    /// а не бросаем исключение посреди уничтожения.
    /// </remarks>
    private EntityLifeState ResolveEndState(bool fullDestroy)
    {
        if (fullDestroy)
            return EntityLifeState.Destroyed;

        Enumeration disposeAction = DisposeAction;

        if (disposeAction == EntityDisposeEnumerations.Destroy)
            return EntityLifeState.Destroyed;

        if (IsHideAction(disposeAction))
            return EntityLifeState.Hidden;

        if (disposeAction == EntityDisposeEnumerations.HideInPool)
        {
            if (PoolBehaviour.IsInitialize)
                return EntityLifeState.Pooled;

            PRLog.WriteWarning(this, $"Entity {EntityType} - {Name} использует настройку {nameof(EntityDisposeEnumerations.HideInPool)}, но при этом создается не через pool system. Объект полностью уничтожен.");

            return EntityLifeState.Destroyed;
        }

        PRLog.WriteError(this, $"Entity {EntityType} - {Name}: для действия при уничтожении '{disposeAction}' нет обработки. Объект полностью уничтожен.");

        return EntityLifeState.Destroyed;
    }

    private static bool IsHideAction(Enumeration disposeAction)
    {
        return disposeAction == EntityDisposeEnumerations.Hide ||
               disposeAction == EntityDisposeEnumerations.HideWire ||
               disposeAction == EntityDisposeEnumerations.HideWirePolygons;
    }

    /// <summary>
    /// Выключает объект сущности; в каркасных режимах оставляет на его месте каркас.
    /// </summary>
    private void HideObject()
    {
        Enumeration disposeAction = DisposeAction;

        bool wire = disposeAction == EntityDisposeEnumerations.HideWire ||
                    disposeAction == EntityDisposeEnumerations.HideWirePolygons;

        // Каркас снимается с видимого объекта: у уже выключенного копировать нечего.
        if (wire && EntityGameObject.activeSelf)
        {
            EntityWireStyle style = disposeAction == EntityDisposeEnumerations.HideWirePolygons
                ? EntityWireStyle.Polygons
                : EntityWireStyle.Triangles;

            wireVisual = EntityWireVisual.Create(EntityGameObject, style, GetWireScale());
        }

        EntityGameObject.SetActive(false);
    }

    /// <summary>
    /// Убирает объект сущности совсем: каркас, учёт в пуле и сам объект.
    /// </summary>
    private void DestroyObject()
    {
        ClearWireVisual();

        // Пул выданный корень уничтожает сам: сущность бывает вложена в него и уничтожила бы только себя.
        OnDestroyPool(true);

        // В редакторе пул уничтожает объект сразу.
        if (this == null)
            return;

        DestroyEntityObject();
    }

    /// <summary>
    /// Уничтожает объект Unity, на котором стоит сущность.
    /// </summary>
    /// <remarks>
    /// В игре уничтожение отложено до конца кадра — до тех пор сущность ещё на сцене в состоянии
    /// <see cref="EntityLifeState.Destroyed"/>. Вне игры <c>Destroy</c> недоступен, объект убирается сразу.
    /// </remarks>
    protected virtual void DestroyEntityObject()
    {
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
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
        CaptureStartTransform();
        BeginEntityLife(newInstance: true, keepAssignedInstance: true);
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
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Сущность зарегистрирована и готова показывать себя.
    /// </summary>
    /// <remarks>
    /// Точка расширения для проектного слоя: интерфейс над сущностью — имя, уровень,
    /// полоса здоровья — создаётся отсюда, а не правкой ядра. Подписчиков может быть
    /// сколько угодно, порядок задаёт <c>Order</c>.
    /// </remarks>
    public const string EntityRegisteredStage = "EntityBaseRegistered";

    protected override void Start()
    {
        RegisterEntity();
        EntityInstanceOwners.Forget(this);
        base.Start();

        this.RunMethodHooks(EntityRegisteredStage);
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        // Объект уничтожила Unity. Если жизнь ещё шла (выгрузка сцены, прямой Destroy, очистка пула), события
        // уничтожения нет: это не смерть в игре, и обработчикам нечего делать на выходе со сцены.
        LifeState = EntityLifeState.Destroyed;
        OnLifeEnded = null;
        EntityInstanceOwners.Forget(this);

        ClearWireVisual();
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

        PoolBehaviour.OnReturnedToPool -= OnReturnedByPool;
        PoolBehaviour.OnReturnedToPool += OnReturnedByPool;
    }

    /// <summary>
    /// Пул выдал объект.
    /// </summary>
    /// <remarks>
    /// Повторная выдача начинает новую жизнь прямо отсюда, а не по подписке из <c>OnEnable</c>: с выключенным
    /// компонентом подписки не было, и сущность выходила из пула с состоянием и записью прошлой жизни.
    /// </remarks>
    public virtual void InitializationPoolObject()
    {
        bool shownAgain = PoolBehaviour.InPool;

        PoolBehaviour.InitializationPoolObject();
        InitializeFromPool(isFirstPool: !shownAgain);
    }

    /// <summary>
    /// Пул забрал объект сам: по таймеру жизни или прямым вызовом, мимо <see cref="DestroyEntity()"/>.
    /// </summary>
    /// <remarks>
    /// Для игры это тот же конец жизни, что и возврат через <c>DestroyEntity</c>: событие приходит, а следующая
    /// выдача начинает жизнь заново. Если сущность вернулась сама, жизнь уже закончена и повторного события нет.
    /// </remarks>
    private void OnReturnedByPool()
    {
        if (TryEndLife(EntityLifeState.Pooled))
            RaiseEntityDestroyed();
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

        BeginEntityLife(newInstance: true);
    }

    /// <summary>
    /// Появление сущности на сцене: создание, выдача из пула, возврат спрятанной.
    /// </summary>
    /// <remarks>
    /// Запись экземпляра ставится до инициализации: она её уже читает. Берётся запись,
    /// переданная через <see cref="EntityInstanceSpawn"/>, а без неё — новая.
    /// </remarks>
    /// <param name="newInstance">
    /// Сущность появляется заново. Возвращённая из спрятанных остаётся тем же экземпляром
    /// и запись не меняет: иначе она теряла бы уровень и получала новый идентификатор.
    /// </param>
    /// <param name="keepAssignedInstance">
    /// Первое появление объекта (<c>Awake</c>). Запись, поставленная до него, остаётся:
    /// объект, созданный выключенным, получает запись раньше, чем просыпается, и замена
    /// её новой стёрла бы сохранённые данные. У объекта из пула такой записи быть не может —
    /// там лежит запись прошлой жизни, и её заменяют всегда.
    /// </param>
    private void BeginEntityLife(bool newInstance, bool keepAssignedInstance = false)
    {
        // Объект уже уничтожается: новой жизни у него не будет.
        if (LifeState == EntityLifeState.Destroyed)
            return;

        LifeState = EntityLifeState.Active;

        // До Start сущности нет в трекере, а запись у неё уже есть: владение записью ищут и здесь.
        if (!HasStarted && this is IEntityInstance)
            EntityInstanceOwners.TrackBeforeStart(this);

        if (newInstance && this is IEntityInstance instance)
        {
            bool assigned = keepAssignedInstance && instance.Instance != null;

            if (!assigned)
            {
                EntityInstanceData fresh = instance.CreateInstance();
                bool nested = IsNestedInstance(fresh?.DefinitionId);

                instance.SetInstance(EntityInstanceSpawn.Take(fresh?.DefinitionId, instance, nested) ?? fresh);
            }
        }

        InitializeEntity();
    }

    /// <summary>
    /// Сущность вложена в другую сущность того же определения.
    /// </summary>
    /// <remarks>
    /// Запись, переданную при создании, такая сущность не берёт: она предназначена внешней.
    /// Проверяется только пока идёт создание с записью, в остальное время ответ не нужен.
    /// </remarks>
    private bool IsNestedInstance(string definitionId)
    {
        if (!EntityInstanceSpawn.IsCreating || transform.parent == null)
            return false;

        IEntityInstance outer = transform.parent.GetComponentInParent<IEntityInstance>(true);

        return outer != null && outer.CreateInstance()?.DefinitionId == definitionId;
    }

    protected virtual void InitializeEntity()
    {

    }

    #endregion

    #region IGameSessionListener

    [field: SerializeField] public EntityDescription Description { get; protected set; }

    protected abstract void InitializeEntityMetadata();

    /// <summary>
    /// Слои времени сущности: какие модификаторы <see cref="PRTimeScale"/> её задевают.
    /// </summary>
    public virtual TimeLayer GetTimeLayers()
    {
        return timeLayers;
    }

    /// <summary>
    /// Возвращает на сцену сущность, спрятанную при уничтожении.
    /// </summary>
    /// <remarks>
    /// Восстанавливается всё и всегда: место, поворот, состояние. Спрашивать сущность,
    /// есть ли ей что чинить, мы не пытаемся — надломленный блок, сдвинутый блок и целый
    /// блок отвечали бы по-разному, а забывший ответить не восстановился бы вовсе и молча.
    /// <para>
    /// Инициализация повторяется, как при выдаче из пула: для сущности это новое появление
    /// на сцене, и состояние прошлой жизни на ней остаться не должно. Объект включается
    /// до инициализации, иначе она не сможет запустить корутины и работать с компонентами.
    /// </para>
    /// </remarks>
    public virtual void RestoreHideEvent(RestoreHideEntitiesEventArgs e)
    {
        // Уничтоженную (в том числе ждущую уничтожения до конца кадра) возвращать некуда.
        if (LifeState == EntityLifeState.Destroyed || !IsHideAction(DisposeAction))
            return;

        ClearWireVisual();

        if (!EntityGameObject.activeSelf)
            EntityGameObject.SetActive(true);

        RestoreStartTransform();
        BeginEntityLife(newInstance: false);
    }

    private void ClearWireVisual()
    {
        wireVisual?.Dispose();
        wireVisual = null;
    }

    /// <summary>
    /// Размер каркаса спрятанной сущности; пусто — текущий размер.
    /// </summary>
    private Vector3? GetWireScale()
    {
        bool original = PRUnitySDK.Settings?.Entity == null ||
                        PRUnitySDK.Settings.Entity.WireScale == EntityWireScaleMode.Original;

        return original && startTransformCaptured ? startScale : null;
    }

    /// <summary>
    /// Запоминает место, поворот и размер, с которыми сущность появилась.
    /// </summary>
    /// <remarks>
    /// До первой инициализации: она уже может сущность подвинуть или раздуть, и тогда
    /// запомнилось бы не то состояние, с которого всё начиналось.
    /// <para>
    /// Размер берётся у <see cref="EntityGameObject"/>, а не у корня иерархии: задают его
    /// именно телу, а корень обычно остаётся единичным.
    /// </para>
    /// </remarks>
    private void CaptureStartTransform()
    {
        if (startTransformCaptured)
            return;

        startPosition = Position;
        startRotation = Rotation;
        startScale = EntityGameObject.transform.localScale;
        startTransformCaptured = true;
    }

    /// <summary>
    /// Возвращает сущность к тому месту, повороту и размеру, с которых она начинала.
    /// </summary>
    /// <remarks>
    /// Блок можно не только разбить: его сталкивают, разворачивают, раздувают зельем.
    /// Уровень, в котором всё цело, но стоит не там и не такого размера, восстановленным
    /// не назовёшь.
    /// </remarks>
    private void RestoreStartTransform()
    {
        if (!startTransformCaptured)
            return;

        SetPositionAndRotation(startPosition, startRotation);

        EntityGameObject.transform.localScale = startScale;
    }

    #endregion
}
