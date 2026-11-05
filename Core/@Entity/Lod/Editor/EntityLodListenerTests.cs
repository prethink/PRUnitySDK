#if PRSDK_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Подписки слушателей детализации: вложенные сущности, перенос слушателя к другой сущности, отписка.
/// </summary>
/// <remarks>
/// В редакторе <c>OnEnable</c> детализации не вызывается: слушателей собирает <see cref="EntityLod.RefreshListeners"/>,
/// перенос — <see cref="EntityLod.Resubscribe"/> (в игре их зовут включение и <c>OnTransformParentChanged</c>), уровень
/// ставится внутренним <c>SetLevel</c>, как это делает служба.
/// </remarks>
public class EntityLodListenerTests
{
    private GameObject root;
    private EntityLod outer;
    private EntityLod inner;
    private EntityLod other;
    private EntityLodTestListener listener;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("LodTestRoot");
        outer = root.AddComponent<EntityLod>();

        var innerObject = new GameObject("Inner");
        innerObject.transform.SetParent(root.transform);
        inner = innerObject.AddComponent<EntityLod>();

        var listenerObject = new GameObject("Listener");
        listenerObject.transform.SetParent(innerObject.transform);
        listener = listenerObject.AddComponent<EntityLodTestListener>();

        var otherObject = new GameObject("Other");
        otherObject.transform.SetParent(root.transform);
        other = otherObject.AddComponent<EntityLod>();
    }

    [TearDown]
    public void TearDown()
    {
        EntityLod.Unsubscribe(listener);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void NestedLod_OnlyNearestNotifies()
    {
        outer.RefreshListeners();
        inner.RefreshListeners();
        outer.RefreshListeners();
        listener.Calls = 0;

        SetLevel(outer, EntityLodLevel.Hidden);
        Assert.That(listener.Calls, Is.Zero, "внешняя детализация не зовёт слушателя вложенной сущности");

        SetLevel(inner, EntityLodLevel.Reduced);
        Assert.That(listener.Calls, Is.EqualTo(1));
        Assert.That(listener.LastLod, Is.SameAs(inner));
    }

    [Test]
    public void MovedListener_FollowsNewLod_AndGetsItsLevel()
    {
        inner.RefreshListeners();
        SetLevel(other, EntityLodLevel.Hidden);

        listener.transform.SetParent(other.transform);
        EntityLod.Resubscribe(listener, listener);

        Assert.That(listener.LastLod, Is.SameAs(other), "при переподписке слушатель сразу узнаёт уровень новой сущности");
        Assert.That(listener.LastLevel, Is.EqualTo(EntityLodLevel.Hidden));

        listener.Calls = 0;
        SetLevel(inner, EntityLodLevel.Reduced);
        Assert.That(listener.Calls, Is.Zero, "прежняя сущность его больше не зовёт");

        SetLevel(other, EntityLodLevel.Full);
        Assert.That(listener.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Unsubscribed_NotNotified()
    {
        inner.RefreshListeners();
        EntityLod.Unsubscribe(listener);
        listener.Calls = 0;

        SetLevel(inner, EntityLodLevel.Hidden);

        Assert.That(listener.Calls, Is.Zero);
    }

    private static void SetLevel(EntityLod lod, EntityLodLevel level)
    {
        MethodInfo setLevel = typeof(EntityLod).GetMethod("SetLevel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        setLevel.Invoke(lod, new object[] { level });
    }
}
#endif
