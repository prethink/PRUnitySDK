#if PRSDK_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Жизненный цикл сущности: уничтожение, возврат спрятанной, пул, вложенность, запись экземпляра.
/// </summary>
/// <remarks>
/// В редакторе Unity не зовёт <c>Awake</c>, <c>Start</c> и <c>OnDestroy</c>, а <c>Destroy</c> недоступен:
/// эти моменты проверка запускает сама (<see cref="EntityLifeTestEntity"/>). Объект, которому в игре
/// досталось бы отложенное уничтожение, здесь остаётся жить — как раз то окно, в котором приходит
/// повторный вызов.
/// </remarks>
public class EntityLifeCycleTests
{
    private const string Prefix = "EntityLifeCycleTests_";

    private EntityTracker previousEntities;
    private EntityTracker entities;
    private ObjectPoolManager previousPool;
    private ObjectPoolManager pool;
    private EntityMetadata metadata;

    [SetUp]
    public void SetUp()
    {
        previousEntities = EntityService.Instance;
        entities = new EntityTracker();
        EntityService.Override(entities);

        previousPool = PRUnitySDK.Managers.ObjectPool;
        pool = new GameObject(Prefix + "Pool").AddComponent<ObjectPoolManager>();
        PRUnitySDK.Managers.ObjectPool = pool;

        metadata = ScriptableObject.CreateInstance<EntityMetadata>();
    }

    [TearDown]
    public void TearDown()
    {
        PRUnitySDK.Managers.ObjectPool = previousPool;
        EntityService.Override(previousEntities);

        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item != null && item.name.StartsWith(Prefix) && !EditorUtility.IsPersistent(item))
                Object.DestroyImmediate(item);
        }

        Object.DestroyImmediate(metadata);
    }

    #region Повторное уничтожение

    [Test]
    public void Destroy_CalledTwice_RaisesEventOnce()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Destroy);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.DestroyEntity();

        // Объект ещё на сцене (Unity уничтожит его в конце кадра), но жизнь уже закончилась.
        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(1, entity.DestroyObjectRequests);
        Assert.AreEqual(EntityLifeState.Destroyed, entity.LifeState);
    }

    [Test]
    public void Hide_CalledTwice_RaisesEventOnce()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.DestroyEntity();

        Assert.IsFalse(entity.gameObject.activeSelf);
        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(EntityLifeState.Hidden, entity.LifeState);
    }

    [Test]
    public void Hide_EnabledByHand_CanBeDestroyedAgain()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        // Игры включают спрятанную вещь напрямую, мимо RestoreHideEntities.
        entity.gameObject.SetActive(true);
        entity.DestroyEntity();

        Assert.AreEqual(2, destroyed);
        Assert.IsFalse(entity.gameObject.activeSelf);
    }

    [Test]
    public void Destroy_CalledAgainFromHandler_RaisesEventOnce()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;

        entity.OnEntityDestroy += value =>
        {
            destroyed++;

            // Объект в этот момент ещё включён: повторный вход не должен принять его за вернувшийся.
            // Счётчик страхует редактор: без защиты здесь бесконечная рекурсия.
            if (destroyed < 5)
                value.DestroyEntity();
        };

        entity.DestroyEntity();

        Assert.AreEqual(1, destroyed);
        Assert.IsFalse(entity.gameObject.activeSelf);
    }

    [Test]
    public void Hide_FullDestroyFromHandler_EndsDestroyed()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;

        entity.OnEntityDestroy += value =>
        {
            destroyed++;

            if (destroyed < 5)
                value.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
        };

        entity.DestroyEntity();

        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(1, entity.DestroyObjectRequests);
        Assert.AreEqual(EntityLifeState.Destroyed, entity.LifeState);
    }

    [Test]
    public void FullDestroy_CalledTwice_RaisesEventOnce()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });

        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(1, entity.DestroyObjectRequests);
    }

    [Test]
    public void FullDestroy_HiddenEntity_RemovesObjectWithoutSecondEvent()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        entity.DestroyObjectNowForTest = true;
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });

        Assert.AreEqual(1, destroyed);
        Assert.IsTrue(entity == null);
    }

    #endregion

    #region Спрятать и вернуть

    [Test]
    public void Hide_ThenRestore_ReturnsToStartAndInitializesAgain()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        EntityInstanceData record = entity.Instance;
        entity.transform.position = new Vector3(5f, 0f, 0f);

        entity.DestroyEntity();
        Assert.IsFalse(entity.gameObject.activeSelf);

        entity.RestoreHideEvent(null);

        Assert.IsTrue(entity.gameObject.activeSelf);
        Assert.AreEqual(EntityLifeState.Active, entity.LifeState);
        Assert.AreEqual(Vector3.zero, entity.transform.position);
        Assert.AreEqual(2, entity.InitializeCount);
        Assert.AreSame(record, entity.Instance, "Вернувшаяся из спрятанных остаётся тем же экземпляром.");
    }

    [Test]
    public void Restore_UntouchedEntity_InitializesAgain()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        EntityInstanceData record = entity.Instance;

        entity.RestoreHideEvent(null);

        Assert.AreEqual(2, entity.InitializeCount);
        Assert.AreSame(record, entity.Instance);
    }

    [Test]
    public void Restore_EntityDisabledByHand_IsEnabled()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        entity.gameObject.SetActive(false);

        entity.RestoreHideEvent(null);

        // Причину отключения восстановление не различает: так сейчас и остаётся.
        Assert.IsTrue(entity.gameObject.activeSelf);
    }

    [Test]
    public void Restore_AfterFullDestroy_DoesNotReviveDyingEntity()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);

        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
        entity.RestoreHideEvent(null);

        // Объект ждёт уничтожения до конца кадра: возвращать его некуда.
        Assert.AreEqual(1, entity.InitializeCount);
        Assert.AreEqual(EntityLifeState.Destroyed, entity.LifeState);
    }

    [Test]
    public void Destroy_EntityWithOtherAction_IsNotRestored()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Destroy);

        entity.RestoreHideEvent(null);

        Assert.AreEqual(1, entity.InitializeCount);
    }

    #endregion

    #region Пул

    [Test]
    public void Pool_DestroyEntity_ReturnsToPoolOnce()
    {
        EntityLifeTestEntity entity = Show(Prefab());
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.DestroyEntity();

        Assert.IsTrue(entity.InPool);
        Assert.IsFalse(entity.gameObject.activeSelf);
        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(EntityLifeState.Pooled, entity.LifeState);
    }

    [Test]
    public void Pool_FullDestroyOfPooledEntity_RemovesItWithoutSecondEvent()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
        EntityLifeTestEntity next = pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreEqual(1, destroyed);
        Assert.IsTrue(entity == null, "Пул уничтожает выданный корень сам.");
        Assert.IsNotNull(next);
        Assert.AreNotSame(entity, next);
    }

    [Test]
    public void Pool_FullDestroyOfShownEntity_RemovesItFromPool()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true });
        EntityLifeTestEntity next = pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreEqual(1, destroyed);
        Assert.IsTrue(entity == null);
        Assert.AreNotSame(entity, next);
    }

    [Test]
    public void Pool_ShownAgain_IsSameObjectAndEventStaysSubscribed()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        EntityLifeTestEntity again = pool.ShowEntity(prefab, Vector3.zero);
        again.DestroyEntity();

        Assert.AreSame(entity, again);
        // Подписка на OnEntityDestroy живёт весь срок компонента, а не одну выдачу.
        Assert.AreEqual(2, destroyed);
    }

    [Test]
    public void Pool_ShownAgain_StartsNewLifeWithNewRecord()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        EntityInstanceData first = entity.Instance;

        entity.DestroyEntity();
        pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreEqual(EntityLifeState.Active, entity.LifeState);
        Assert.AreEqual(2, entity.InitializeCount);
        Assert.AreNotSame(first, entity.Instance);
        Assert.AreNotEqual(first.InstanceId, entity.Instance.InstanceId);
    }

    [Test]
    public void Pool_FirstShow_DoesNotInitializeTwice()
    {
        EntityLifeTestEntity entity = Show(Prefab());

        // Первая выдача продолжает жизнь, начатую в Awake.
        Assert.AreEqual(1, entity.InitializeCount);
        Assert.AreEqual(EntityLifeState.Active, entity.LifeState);
    }

    [Test]
    public void Pool_DisabledComponentShownAgain_StartsNewLife()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);

        entity.DestroyEntity();
        entity.enabled = false;
        pool.ShowEntity(prefab, Vector3.zero);

        // Раньше выдачу слушала подписка из OnEnable, и выключенный компонент её пропускал.
        Assert.AreEqual(2, entity.InitializeCount);
        Assert.AreEqual(EntityLifeState.Active, entity.LifeState);
    }

    [Test]
    public void Pool_ReturnedByPoolItself_EndsLifeOnce()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        // Так возвращают LuckyBox и таймер жизни пула: мимо DestroyEntity.
        pool.OnObjectDestroy(entity.gameObject);

        entity.DestroyEntity();

        Assert.IsFalse(entity.gameObject.activeSelf);
        Assert.IsTrue(entity.InPool);
        Assert.AreEqual(EntityLifeState.Pooled, entity.LifeState);
        Assert.AreEqual(1, destroyed);

        pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreEqual(2, entity.InitializeCount, "Следующая выдача — новая жизнь, а не продолжение прошлой.");
    }

    [Test]
    public void Pool_ClearData_DestroysEntitiesWithoutEvent()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity shown = Show(prefab);
        EntityLifeTestEntity returned = Show(prefab);
        int destroyed = 0;
        shown.OnEntityDestroy += _ => destroyed++;
        returned.OnEntityDestroy += _ => destroyed++;
        returned.DestroyEntity();
        destroyed = 0;

        pool.ClearData();

        Assert.IsTrue(shown == null);
        Assert.IsTrue(returned == null);
        // Очистка пула — уничтожение объекта Unity, а не смерть в игре.
        Assert.AreEqual(0, destroyed);
    }

    #endregion

    #region Подписки

    [Test]
    public void LifeEnded_SubscriptionLastsOneLife()
    {
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity entity = Show(prefab);
        int lifeEnded = 0;
        int destroyed = 0;
        entity.OnLifeEnded += _ => lifeEnded++;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        pool.ShowEntity(prefab, Vector3.zero);
        entity.DestroyEntity();

        Assert.AreEqual(1, lifeEnded, "Подписчик прошлой жизни о следующей не узнаёт.");
        Assert.AreEqual(2, destroyed, "Подписчик на весь срок компонента узнаёт о каждой.");
    }

    [Test]
    public void LifeEnded_SubscribedFromHandler_BelongsToNextLife()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int lifeEnded = 0;

        void OnLifeEnded(EntityBase value)
        {
            lifeEnded++;
            value.OnLifeEnded += OnLifeEnded;
        }

        entity.OnLifeEnded += OnLifeEnded;

        entity.DestroyEntity();
        Assert.AreEqual(1, lifeEnded);

        entity.RestoreHideEvent(null);
        entity.DestroyEntity();

        Assert.AreEqual(2, lifeEnded);
    }

    [Test]
    public void LifeEnded_ComesOnceWhenPoolTakesEntityItself()
    {
        EntityLifeTestEntity entity = Show(Prefab());
        int lifeEnded = 0;
        entity.OnLifeEnded += _ => lifeEnded++;

        pool.OnObjectDestroy(entity.gameObject);
        entity.DestroyEntity();

        Assert.AreEqual(1, lifeEnded);
    }

    [Test]
    public void LifeEnded_UnityDestroy_DropsSubscribersWithoutCall()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int lifeEnded = 0;
        entity.OnLifeEnded += _ => lifeEnded++;

        entity.UnityDestroyedForTest();
        entity.DestroyEntity();

        Assert.AreEqual(0, lifeEnded);
    }

    #endregion

    #region Вложенная сущность

    [Test]
    public void Nested_ParentHiddenAndRestored_ChildLivesOwnLife()
    {
        EntityLifeTestEntity parent = Add(EntityDisposeEnumerations.Hide);
        EntityLifeTestEntity child = Add(EntityDisposeEnumerations.Hide, parent: parent.transform);
        int childDestroyed = 0;
        child.OnEntityDestroy += _ => childDestroyed++;

        parent.DestroyEntity();
        parent.RestoreHideEvent(null);

        Assert.AreEqual(0, childDestroyed);
        Assert.AreEqual(1, child.InitializeCount);
        Assert.IsTrue(child.gameObject.activeSelf);
    }

    [Test]
    public void Nested_ChildDestroyed_ParentUntouched()
    {
        EntityLifeTestEntity parent = Add(EntityDisposeEnumerations.Hide);
        EntityLifeTestEntity child = Add(EntityDisposeEnumerations.Hide, parent: parent.transform);
        int parentDestroyed = 0;
        parent.OnEntityDestroy += _ => parentDestroyed++;

        child.DestroyEntity();

        Assert.AreEqual(0, parentDestroyed);
        Assert.IsTrue(parent.gameObject.activeSelf);
        Assert.IsFalse(child.gameObject.activeSelf);
    }

    #endregion

    #region До Start

    [Test]
    public void BeforeStart_DestroyEntity_WorksWithoutRegistration()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.DestroyEntity();
        entity.UnityDestroyedForTest();

        Assert.AreEqual(1, destroyed);
        Assert.AreEqual(0, entities.GetEntitiesCount());
    }

    [Test]
    public void BeforeAwake_DestroyEntity_HidesInactiveObject()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Hide, awake: false);
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        Assert.AreEqual(EntityLifeState.Created, entity.LifeState);

        entity.DestroyEntity();

        Assert.AreEqual(1, destroyed);
        Assert.IsFalse(entity.gameObject.activeSelf);
        Assert.AreEqual(EntityLifeState.Hidden, entity.LifeState);
    }

    [Test]
    public void UnityDestroy_DoesNotRaiseEvent()
    {
        EntityLifeTestEntity entity = Add(EntityDisposeEnumerations.Destroy);
        entity.StartForTest();
        int destroyed = 0;
        entity.OnEntityDestroy += _ => destroyed++;

        entity.UnityDestroyedForTest();

        // Выгрузка сцены и прямой Destroy о смерти не сообщают: обработчики не должны спавнить на выходе.
        Assert.AreEqual(0, destroyed);
        Assert.AreEqual(0, entities.GetEntitiesCount());
        Assert.AreEqual(EntityLifeState.Destroyed, entity.LifeState);
    }

    #endregion

    #region Запись экземпляра

    [Test]
    public void Instance_TwoCreationsBeforeStart_SecondIsRefused()
    {
        EntityInstanceData record = EntityInstanceData.Create("life-test");
        EntityLifeTestEntity first = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Destroy));
        bool factoryCalled = false;

        EntityInstanceSpawn.Create(record, () =>
        {
            factoryCalled = true;
            return Add(EntityDisposeEnumerations.Destroy);
        });

        // MAINT-003: до Start первой сущности нет в трекере, но запись уже за ней.
        Assert.AreSame(record, first.Instance);
        Assert.IsFalse(factoryCalled);
    }

    [Test]
    public void Instance_GivenToInactiveObject_SecondCreationIsRefused()
    {
        EntityInstanceData record = EntityInstanceData.Create("life-test");
        EntityLifeTestEntity sleeping = EntityInstanceSpawn.Create(record,
            () => Add(EntityDisposeEnumerations.Destroy, awake: false));
        bool factoryCalled = false;

        EntityInstanceSpawn.Create(record, () =>
        {
            factoryCalled = true;
            return Add(EntityDisposeEnumerations.Destroy);
        });

        Assert.AreSame(record, sleeping.Instance);
        Assert.IsFalse(factoryCalled);
    }

    [Test]
    public void Instance_ReusedInFrameOfDestroy_GoesToNextEntity()
    {
        EntityInstanceData record = EntityInstanceData.Create("life-test");
        EntityLifeTestEntity owner = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Destroy));
        owner.StartForTest();

        owner.DestroyEntity();
        EntityLifeTestEntity next = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Destroy));

        // Прежняя сущность ещё на сцене до конца кадра, но её жизнь закончилась и запись свободна.
        Assert.IsNotNull(next);
        Assert.AreSame(record, next.Instance);
    }

    [Test]
    public void Instance_HiddenOwner_KeepsRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create("life-test");
        EntityLifeTestEntity owner = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Hide));
        owner.StartForTest();

        owner.DestroyEntity();
        EntityLifeTestEntity next = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Hide));

        // Спрятанную сущность вернут тем же экземпляром, поэтому запись остаётся за ней.
        Assert.IsNull(next);
        Assert.AreSame(record, owner.Instance);
    }

    [Test]
    public void Instance_OwnerInPool_RecordGoesToNextEntity()
    {
        EntityInstanceData record = EntityInstanceData.Create("life-test");
        EntityLifeTestEntity prefab = Prefab();
        EntityLifeTestEntity owner = EntityInstanceSpawn.Create(record, () => Show(prefab));
        owner.StartForTest();

        owner.DestroyEntity();
        EntityLifeTestEntity next = EntityInstanceSpawn.Create(record, () => Add(EntityDisposeEnumerations.Destroy));

        Assert.AreSame(record, owner.Instance, "Проверка имеет смысл, пока запись прошлой жизни ещё лежит у сущности в пуле.");
        Assert.IsNotNull(next);
        Assert.AreSame(record, next.Instance);
    }

    #endregion

    private EntityLifeTestEntity Add(Enumeration dispose, bool awake = true, Transform parent = null)
    {
        // Уничтожение в редакторе пишет ошибку «Destroy may not be called from edit mode», отказ в записи —
        // ошибку SDK. Флаг действует только на текущий тест, поэтому ставится из его тела.
        LogAssert.ignoreFailingMessages = true;

        var gameObject = new GameObject(Prefix + "Entity");

        if (parent != null)
            gameObject.transform.SetParent(parent, false);

        if (!awake)
            gameObject.SetActive(false);

        var entity = gameObject.AddComponent<EntityLifeTestEntity>();
        entity.SetDisposeForTest(dispose);

        if (awake)
            entity.AwakeForTest(metadata);

        return entity;
    }

    /// <summary>
    /// Заготовка вместо префаба: выключенный объект с возвратом в пул.
    /// </summary>
    private EntityLifeTestEntity Prefab()
    {
        LogAssert.ignoreFailingMessages = true;

        var gameObject = new GameObject(Prefix + "Prefab");
        gameObject.SetActive(false);

        var prefab = gameObject.AddComponent<EntityLifeTestEntity>();
        prefab.SetDisposeForTest(EntityDisposeEnumerations.HideInPool);

        return prefab;
    }

    /// <summary>
    /// Первая выдача из пула: объект создан, включён и прошёл свой <c>Awake</c>.
    /// </summary>
    private EntityLifeTestEntity Show(EntityLifeTestEntity prefab)
    {
        EntityLifeTestEntity entity = pool.ShowEntity(prefab, Vector3.zero);
        entity.AwakeForTest(metadata);

        return entity;
    }
}
#endif
