#if PRSDK_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Пул отдаёт экземпляр именно того префаба, который попросили.
/// </summary>
/// <remarks>
/// У разных префабов часто совпадает всё, по чему раньше строился ключ пула: у префаба
/// сущности нет метаданных (они ставятся в <c>Awake</c>, а ассет не просыпается), и
/// <see cref="EntityBase.GetPoolKey"/> у всех даёт <c>Unknown</c>; у обычных префабов
/// совпадает тип компонента или имя. Второй префаб тогда молча получал экземпляры первого.
/// </remarks>
public class ObjectPoolPrefabIdentityTests
{
    private const string Prefix = "ObjectPoolPrefabIdentityTests_";

    private ObjectPoolManager pool;

    [SetUp]
    public void SetUp()
    {
        pool = new GameObject(Prefix + "Pool").AddComponent<ObjectPoolManager>();
    }

    [TearDown]
    public void TearDown()
    {
        // ClearData пула зовёт Destroy, а в редакторе он недоступен: всё созданное
        // (префабы, экземпляры, сам пул) убираем сами — по префиксу имени.
        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item != null && item.name.StartsWith(Prefix) && !EditorUtility.IsPersistent(item))
                Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void ShowEntity_TwoPrefabsWithSamePoolKey_EachGetsOwnInstance()
    {
        Entity first = CreatePrefab<Entity>("First");
        Entity second = CreatePrefab<Entity>("Second");
        Assert.AreEqual(first.GetPoolKey(), second.GetPoolKey(), "Проверка имеет смысл только при совпадающем ключе.");

        Entity fromFirst = pool.ShowEntity(first, Vector3.zero);
        Entity fromSecond = pool.ShowEntity(second, Vector3.zero);

        Assert.AreEqual(first.name + "(Clone)", fromFirst.name);
        Assert.AreEqual(second.name + "(Clone)", fromSecond.name);
    }

    [Test]
    public void ShowEntity_ReturnedInstance_ComesBackForItsOwnPrefab()
    {
        Entity first = CreatePrefab<Entity>("First");
        Entity second = CreatePrefab<Entity>("Second");

        Entity fromFirst = pool.ShowEntity(first, Vector3.zero);
        pool.OnObjectDestroy(fromFirst.gameObject);

        // Свободный экземпляр первого не должен достаться второму префабу.
        Entity fromSecond = pool.ShowEntity(second, Vector3.zero);
        Entity firstAgain = pool.ShowEntity(first, Vector3.zero);

        Assert.AreEqual(second.name + "(Clone)", fromSecond.name);
        Assert.AreSame(fromFirst, firstAgain);
    }

    [Test]
    public void ShowObject_TwoPrefabsWithSameComponentType_EachGetsOwnInstance()
    {
        BoxCollider first = CreatePrefab<BoxCollider>("First");
        BoxCollider second = CreatePrefab<BoxCollider>("Second");

        BoxCollider fromFirst = pool.ShowObject(first, Vector3.zero, out _);
        BoxCollider fromSecond = pool.ShowObject(second, Vector3.zero, out _);

        Assert.AreEqual(first.name + "(Clone)", fromFirst.name);
        Assert.AreEqual(second.name + "(Clone)", fromSecond.name);
    }

    [Test]
    public void ShowObject_TwoPrefabsWithSameName_EachGetsOwnInstance()
    {
        GameObject first = CreatePrefab<Transform>("Twin").gameObject;
        GameObject second = CreatePrefab<Transform>("Twin").gameObject;
        second.AddComponent<BoxCollider>();

        GameObject fromFirst = pool.ShowObject(first, Vector3.zero, out _);
        GameObject fromSecond = pool.ShowObject(second, Vector3.zero, out _);

        Assert.IsFalse(fromFirst.TryGetComponent<BoxCollider>(out _));
        Assert.IsTrue(fromSecond.TryGetComponent<BoxCollider>(out _));
    }

    [Test]
    public void ShowObject_GameObjectAndComponentOfSamePrefab_ShareOnePool()
    {
        BoxCollider prefab = CreatePrefab<BoxCollider>("Shared");

        GameObject asObject = pool.ShowObject(prefab.gameObject, Vector3.zero, out _);
        pool.OnObjectDestroy(asObject);
        BoxCollider asComponent = pool.ShowObject(prefab, Vector3.zero, out _);

        Assert.AreSame(asObject, asComponent.gameObject);
    }

    [Test]
    public void ShowEntity_RepeatedShowAndReturn_ReusesOneInstance()
    {
        Entity prefab = CreatePrefab<Entity>("Repeated");

        for (int i = 0; i < 5; i++)
            pool.OnObjectDestroy(pool.ShowEntity(prefab, Vector3.zero).gameObject);

        Assert.AreEqual(1, CountInstances(prefab));
    }

    [Test]
    public void ShowEntity_WithoutParentAfterParentedShow_DoesNotKeepOldParent()
    {
        Entity prefab = CreatePrefab<Entity>("Parented");
        Transform oldParent = new GameObject(Prefix + "OldParent").transform;

        Entity first = pool.ShowEntity(prefab, oldParent);
        pool.OnObjectDestroy(first.gameObject);
        Entity again = pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreSame(first, again, "Проверка про повторную выдачу того же экземпляра.");
        Assert.AreNotSame(oldParent, again.transform.parent);
    }

    [Test]
    public void OnObjectDestroy_MovesInstanceBackToPoolContainer()
    {
        Entity prefab = CreatePrefab<Entity>("Returned");
        Transform oldParent = new GameObject(Prefix + "OldParent").transform;

        Entity shown = pool.ShowEntity(prefab, oldParent);
        pool.OnObjectDestroy(shown.gameObject);

        Assert.AreNotSame(oldParent, shown.transform.parent);
    }

    [Test]
    public void ShowEntity_OldParentDestroyedWhileInPool_KeepsPooledInstance()
    {
        Entity prefab = CreatePrefab<Entity>("Orphan");
        var oldParent = new GameObject(Prefix + "OldParent");

        Entity first = pool.ShowEntity(prefab, oldParent.transform);
        pool.OnObjectDestroy(first.gameObject);
        Object.DestroyImmediate(oldParent);

        Entity again = pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreSame(first, again, "Свободный экземпляр не должен погибнуть вместе с родителем прошлой выдачи.");
        Assert.AreEqual(1, CountInstances(prefab));
    }

    [Test]
    public void OnObjectDestroy_FullDestroy_DestroysInstance()
    {
        BoxCollider prefab = CreatePrefab<BoxCollider>("FullDestroy");

        pool.ShowObject(prefab, Vector3.zero, out PoolObject poolObject);
        pool.OnObjectDestroy(poolObject, fullDestroy: true);

        Assert.AreEqual(0, CountInstances(prefab));
    }

    [Test]
    public void RegisterEntityPool_CountsInstancesAlreadyShown()
    {
        Entity prefab = CreatePrefab<Entity>("Busy");

        for (int i = 0; i < 3; i++)
            pool.ShowEntity(prefab, Vector3.zero);

        // Три экземпляра уже есть, хоть и выданы: прогреву до трёх создавать нечего.
        pool.RegisterEntityPool(prefab, 3);

        Assert.AreEqual(3, CountInstances(prefab));
    }

    [Test]
    public void RegisterEntityPool_Repeated_DoesNotRestartFilling()
    {
        Entity prefab = CreatePrefab<Entity>("Twice");

        // Первый прогрев создал один экземпляр и ждёт кадров на остальные два.
        pool.RegisterEntityPool(prefab, 3);
        pool.RegisterEntityPool(prefab, 3);

        Assert.AreEqual(1, CountInstances(prefab));
    }

    [Test]
    public void ShowEntity_UsesInstancesFromRegisterEntityPool()
    {
        Entity prefab = CreatePrefab<Entity>("Prewarmed");

        // В редакторе корутина наполнения успевает только первый шаг: один экземпляр.
        pool.RegisterEntityPool(prefab, 3);
        int prewarmed = CountInstances(prefab);
        Entity shown = pool.ShowEntity(prefab, Vector3.zero);

        Assert.AreEqual(1, prewarmed);
        Assert.AreEqual(1, CountInstances(prefab), "Выдача должна взять прогретый экземпляр, а не создать новый.");
        Assert.IsTrue(shown != null);
    }

    [Test]
    public void ShowObject_UsesInstancesFromRegisterPrefabPool()
    {
        BoxCollider prefab = CreatePrefab<BoxCollider>("PrewarmedObject");

        pool.RegisterPrefabPool(prefab.gameObject, 3);
        pool.ShowObject(prefab, Vector3.zero, out _);

        Assert.AreEqual(1, CountInstances(prefab));
    }

    [Test]
    public void ShowObject_DuringFilling_TakesNextPrefabInTurn()
    {
        GameObject first = CreatePrefab<Transform>("TurnA").gameObject;
        GameObject second = CreatePrefab<Transform>("TurnB").gameObject;

        // По два каждого: наполнение успело создать первый, следующий по кругу — второй.
        pool.RegisterPoolObject(Prefix + "Type", "Turn", new System.Collections.Generic.List<GameObject> { first, second }, 2);
        pool.ShowObject(Prefix + "Type", "Turn", Vector3.zero, Quaternion.identity, Vector3.one, null);

        // Случайный выбор для этого зерна дал бы первый префаб. Общий генератор возвращаем как был:
        // от него могут зависеть другие проверки.
        Random.State saved = Random.state;
        PoolObject onDemand;

        try
        {
            Random.InitState(SeedPickingFirstOfTwo());
            onDemand = pool.ShowObject(Prefix + "Type", "Turn", Vector3.zero, Quaternion.identity, Vector3.one, null);
        }
        finally
        {
            Random.state = saved;
        }

        Assert.AreEqual(second.name + "(Clone)", onDemand.InstanceGameObject.name);
    }

    [Test]
    public void ShowObject_WithoutParent_PlacesAtWorldPosition()
    {
        // Менеджер пула создаётся из префаба со сдвинутым корнем: контейнер не в начале координат.
        MovePoolContainer(new Vector3(-14f, 3f, -65f));
        BoxCollider prefab = CreatePrefab<BoxCollider>("World");
        var target = new Vector3(1f, 2f, 3f);

        BoxCollider first = pool.ShowObject(prefab, target, out _);
        Vector3 firstPosition = first.transform.position;
        pool.OnObjectDestroy(first.gameObject);
        BoxCollider again = pool.ShowObject(prefab, target, out _);

        Assert.AreEqual(target, firstPosition);
        Assert.AreEqual(target, again.transform.position);
    }

    [Test]
    public void ShowEntity_WithoutParent_PlacesAtWorldPosition()
    {
        MovePoolContainer(new Vector3(-14f, 3f, -65f));
        Entity prefab = CreatePrefab<Entity>("WorldEntity");
        var target = new Vector3(1f, 2f, 3f);

        Entity shown = pool.ShowEntity(prefab, target);

        Assert.AreEqual(target, shown.transform.position);
    }

    [Test]
    public void ShowObject_AtTransform_BecomesChildAtItsPlace()
    {
        GameObject prefab = CreatePrefab<Transform>("AtTransform").gameObject;
        Transform anchor = new GameObject(Prefix + "Anchor").transform;
        anchor.position = new Vector3(5f, 0f, 0f);

        pool.RegisterPoolObject(Prefix + "Anchored", prefab);
        PoolObject shown = pool.ShowObject(Prefix + "Anchored", anchor);

        Assert.AreSame(anchor, shown.InstanceGameObject.transform.parent);
        Assert.AreEqual(anchor.position, shown.InstanceGameObject.transform.position);
    }

    /// <summary>
    /// Ставит контейнер свободных объектов пула в заданную точку, как у созданного из префаба менеджера.
    /// </summary>
    private void MovePoolContainer(Vector3 position)
    {
        Transform container = new GameObject(Prefix + "Container").transform;
        container.position = position;

        var serialized = new SerializedObject(pool);
        serialized.FindProperty("gameObjects").objectReferenceValue = container;
        serialized.FindProperty("particles").objectReferenceValue = container;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int SeedPickingFirstOfTwo()
    {
        for (int seed = 0; ; seed++)
        {
            Random.InitState(seed);
            if (Random.Range(0, 2) == 0)
                return seed;
        }
    }

    private static int CountInstances(Component prefab)
    {
        string name = prefab.name + "(Clone)";
        int count = 0;

        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item != null && item.name == name && !EditorUtility.IsPersistent(item))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Заготовка вместо префаба: выключенный объект, чтобы у копий не было Awake.
    /// </summary>
    private static T CreatePrefab<T>(string name)
        where T : Component
    {
        var gameObject = new GameObject(Prefix + name);
        gameObject.SetActive(false);

        return gameObject.TryGetComponent(out T existing) ? existing : gameObject.AddComponent<T>();
    }
}
#endif
