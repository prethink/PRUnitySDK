#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Событие только для тестов шины: настоящие подписки проекта эти тесты не задевают.
/// </summary>
public interface IEventBusTestEvent : IGlobalSubscriber
{
    void OnTestEvent(List<string> log);
}

/// <summary>
/// Учёт подписчиков <see cref="EventBus"/>: порядок доставки, дубликаты, отписка, повторная
/// подписка до пересборки списка и уничтоженные Unity-объекты.
/// </summary>
public class EventBusSubscriptionTests
{
    private readonly List<IGlobalSubscriber> created = new();
    private readonly List<ScriptableObject> assets = new();
    private List<string> log;

    [SetUp]
    public void SetUp()
    {
        log = new List<string>();
        Assert.AreEqual(0, EventBus.GetSubscriberCount<IEventBusTestEvent>(),
            "Подписки на тестовое событие остались от прошлого теста.");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (IGlobalSubscriber subscriber in created)
            EventBus.Unsubscribe(subscriber);

        foreach (ScriptableObject asset in assets)
        {
            if (asset != null)
                UnityEngine.Object.DestroyImmediate(asset);
        }

        created.Clear();
        assets.Clear();
    }

    [Test]
    public void Raise_DeliversInSubscriptionOrder()
    {
        Subscribe("a");
        Subscribe("b");
        Subscribe("c");

        Raise();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log);
    }

    [Test]
    public void Subscribe_SameInstanceTwice_IsIgnored()
    {
        Recorder a = Subscribe("a");

        Assert.IsFalse(EventBus.Subscribe(a));
        Raise();

        CollectionAssert.AreEqual(new[] { "a" }, log);
        Assert.AreEqual(1, EventBus.GetSubscriberCount<IEventBusTestEvent>());
    }

    [Test]
    public void Unsubscribe_StopsDelivery_AndKeepsOrderOfOthers()
    {
        Subscribe("a");
        Recorder b = Subscribe("b");
        Subscribe("c");
        Recorder d = Subscribe("d");
        Subscribe("e");

        Assert.IsTrue(EventBus.Unsubscribe(b));
        Assert.IsTrue(EventBus.Unsubscribe(d));
        Assert.IsFalse(EventBus.Unsubscribe(d), "Повторная отписка не должна находить подписчика.");
        Raise();

        CollectionAssert.AreEqual(new[] { "a", "c", "e" }, log);
        Assert.AreEqual(3, EventBus.GetSubscriberCount<IEventBusTestEvent>());
    }

    [Test]
    public void Resubscribe_BeforeNextRaise_DeliversOnce()
    {
        Subscribe("a");
        Recorder b = Subscribe("b");
        Raise();
        log.Clear();

        // Отписка откладывает вычёркивание из списка до пересборки - повторная подписка
        // в этом окне не должна задвоить запись.
        EventBus.Unsubscribe(b);
        Assert.IsTrue(EventBus.Subscribe(b));
        Raise();

        CollectionAssert.AreEqual(new[] { "a", "b" }, log);
        Assert.AreEqual(2, EventBus.GetSubscriberCount<IEventBusTestEvent>());
    }

    [Test]
    public void Resubscribe_AfterRaise_MovesToEnd()
    {
        Recorder a = Subscribe("a");
        Subscribe("b");

        EventBus.Unsubscribe(a);
        Raise();
        log.Clear();

        EventBus.Subscribe(a);
        Raise();

        CollectionAssert.AreEqual(new[] { "b", "a" }, log);
    }

    [Test]
    public void DestroyedUnitySubscriber_IsSkipped_AndDropped()
    {
        Subscribe("a");
        TestAsset asset = SubscribeAsset("asset");
        Subscribe("c");

        UnityEngine.Object.DestroyImmediate(asset);
        Raise();

        CollectionAssert.AreEqual(new[] { "a", "c" }, log);
        Assert.AreEqual(2, EventBus.GetSubscriberCount<IEventBusTestEvent>());
    }

    [Test]
    public void Subscribe_DestroyedUnityObject_IsRejected()
    {
        var asset = ScriptableObject.CreateInstance<TestAsset>();
        UnityEngine.Object.DestroyImmediate(asset);

        // Шина пишет предупреждение - Test Runner его не считает провалом.
        Assert.IsFalse(EventBus.Subscribe(asset));
        Assert.AreEqual(0, EventBus.GetSubscriberCount<IEventBusTestEvent>());
    }

    [Test]
    public void UnsubscribeDuringRaise_TakesEffectFromNextRaise()
    {
        Recorder b = null;
        Subscribe("a", () => EventBus.Unsubscribe(b));
        b = Subscribe("b");

        Raise();
        Assert.AreEqual(new[] { "a", "b" }, log.ToArray(),
            "Публикация идёт по снимку: отписанный в ней подписчик ещё получает это событие.");

        log.Clear();
        Raise();
        CollectionAssert.AreEqual(new[] { "a" }, log);
    }

    [Test]
    public void SubscribeDuringRaise_TakesEffectFromNextRaise()
    {
        var late = new Recorder("late", log, null);
        Subscribe("a", () =>
        {
            if (EventBus.Subscribe(late))
                created.Add(late);
        });

        Raise();
        CollectionAssert.AreEqual(new[] { "a" }, log);

        log.Clear();
        Raise();
        CollectionAssert.AreEqual(new[] { "a", "late" }, log);
    }

    [Test]
    public void ManySubscribers_SubscribeAndUnsubscribeAll()
    {
        const int count = 3000;
        var recorders = new List<Recorder>(count);

        for (var i = 0; i < count; i++)
            recorders.Add(Subscribe(i.ToString()));

        Assert.AreEqual(count, EventBus.GetSubscriberCount<IEventBusTestEvent>());

        // Отписываем каждого второго - оставшиеся должны идти в исходном порядке.
        for (var i = 0; i < count; i += 2)
            EventBus.Unsubscribe(recorders[i]);

        Raise();

        Assert.AreEqual(count / 2, log.Count);
        for (var i = 0; i < log.Count; i++)
            Assert.AreEqual((i * 2 + 1).ToString(), log[i]);
    }

    private Recorder Subscribe(string name, Action onEvent = null)
    {
        var recorder = new Recorder(name, log, onEvent);
        Assert.IsTrue(EventBus.Subscribe(recorder));
        created.Add(recorder);
        return recorder;
    }

    private TestAsset SubscribeAsset(string name)
    {
        var asset = ScriptableObject.CreateInstance<TestAsset>();
        asset.name = name;
        asset.Log = log;
        assets.Add(asset);
        Assert.IsTrue(EventBus.Subscribe(asset));
        created.Add(asset);
        return asset;
    }

    private void Raise()
    {
        EventBus.RaiseEvent<IEventBusTestEvent>(x => x.OnTestEvent(log));
    }

    private sealed class Recorder : IEventBusTestEvent
    {
        private readonly string name;
        private readonly List<string> log;
        private readonly Action onEvent;

        public Recorder(string name, List<string> log, Action onEvent)
        {
            this.name = name;
            this.log = log;
            this.onEvent = onEvent;
        }

        public void OnTestEvent(List<string> target)
        {
            log.Add(name);
            onEvent?.Invoke();
        }
    }

    private sealed class TestAsset : ScriptableObject, IEventBusTestEvent
    {
        public List<string> Log;

        public void OnTestEvent(List<string> target)
        {
            Log?.Add(name);
        }
    }
}
#endif
