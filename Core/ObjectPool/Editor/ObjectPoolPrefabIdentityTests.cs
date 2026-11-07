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
