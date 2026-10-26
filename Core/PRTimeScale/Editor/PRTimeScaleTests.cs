#if PRSDK_TESTS
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Слои времени флагами: кого модификатор задевает, как масштабы перемножаются и что сообщает событие.
/// </summary>
public class PRTimeScaleTests
{
    private PRTimeScale previous;
    private PRTimeScale timeScale;
    private Listener listener;

    [SetUp]
    public void SetUp()
    {
        previous = PRTimeScale.Instance;
        timeScale = new PRTimeScale();
        timeScale.SingletonInitialize();
        PRTimeScale.Override(timeScale);

        listener = new Listener();
        EventBus.Subscribe(listener);
    }

    [TearDown]
    public void TearDown()
    {
        EventBus.Unsubscribe(listener);
        EventBus.Unsubscribe(timeScale);
        PRTimeScale.Override(previous);
    }

    [Test]
    public void Kronos_SlowsWorldAndEnemies_NotPlayerOrUi()
    {
        timeScale.AddModifier(0.3f, TimeLayer.Global, TimeLayer.Player | TimeLayer.UI);

        Assert.AreEqual(0.3f, timeScale.GetWorldTimeScale(), 1e-5f);
        Assert.AreEqual(0.3f, timeScale.Resolve(TimeLayer.NPC | TimeLayer.Enemy), 1e-5f);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.Player), 1e-5f);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.UI), 1e-5f);

        // Физика идёт в темпе мира — игрок догоняет обычный темп множителем больше единицы.
        Assert.AreEqual(1f / 0.3f, timeScale.RelativeToWorld(TimeLayer.Player), 1e-4f);
    }

    [Test]
    public void Modifier_HitsObjectThroughAnyCommonLayer()
    {
        timeScale.AddModifier(0.5f, TimeLayer.Enemy);

        Assert.AreEqual(0.5f, timeScale.Resolve(TimeLayer.NPC | TimeLayer.Enemy), 1e-5f);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.NPC), 1e-5f);
        Assert.AreEqual(1f, timeScale.GetWorldTimeScale(), 1e-5f);
    }

    [Test]
    public void Modifiers_Multiply_AndOwnerRemovesOnlyOwn()
    {
        object ability = new();
        object finisher = new();
        timeScale.AddModifier(0.5f, owner: ability);
        timeScale.AddModifier(0.5f, TimeLayer.World, owner: finisher);

        Assert.AreEqual(0.25f, timeScale.GetWorldTimeScale(), 1e-5f);
        Assert.AreEqual(0.5f, timeScale.Resolve(TimeLayer.Player), 1e-5f);

        Assert.AreEqual(1, timeScale.RemoveModifiers(ability));
        Assert.AreEqual(0.5f, timeScale.GetWorldTimeScale(), 1e-5f);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.Player), 1e-5f);
    }

    [Test]
    public void NpcGroup_HitsBotsPetsMobs_SingleLayerOnlyItself()
    {
        timeScale.AddModifier(0.5f, TimeLayer.NPC);
        timeScale.AddModifier(0.5f, TimeLayer.Pets);

        Assert.AreEqual(0.5f, timeScale.Resolve(TimeLayer.Bots), 1e-5f);
        Assert.AreEqual(0.5f, timeScale.Resolve(TimeLayer.Mobs | TimeLayer.Enemy), 1e-5f);
        Assert.AreEqual(0.25f, timeScale.Resolve(TimeLayer.Pets), 1e-5f);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.Player), 1e-5f);
        Assert.AreEqual(1f, timeScale.GetWorldTimeScale(), 1e-5f);
    }

    [Test]
    public void Except_ExcludesWholeObject_EvenWithOtherLayers()
    {
        timeScale.AddModifier(0.3f, TimeLayer.Global, TimeLayer.Enemy);
        timeScale.AddModifier(0.5f, TimeLayer.Global, TimeLayer.Player);

        // Враг-моб и игрок с эффектом не задеты своими исключениями, хотя второй их слой в Affects есть.
        Assert.AreEqual(0.5f, timeScale.Resolve(TimeLayer.Mobs | TimeLayer.Enemy), 1e-5f);
        Assert.AreEqual(0.3f, timeScale.Resolve(TimeLayer.Player | TimeLayer.Effects), 1e-5f);
        Assert.AreEqual(0.15f, timeScale.Resolve(TimeLayer.Mobs), 1e-5f);
    }

    [Test]
    public void Touches_TreatsNoLayersAsWorld()
    {
        Assert.IsTrue(PRTimeScale.Touches(TimeLayer.World, TimeLayer.None));
        Assert.IsFalse(PRTimeScale.Touches(TimeLayer.Pets, TimeLayer.None));
        Assert.IsTrue(PRTimeScale.Touches(TimeLayer.NPC, TimeLayer.Bots | TimeLayer.Enemy));
    }

    [Test]
    public void NoLayers_CountAsWorld()
    {
        timeScale.AddModifier(0.4f, TimeLayer.World);

        Assert.AreEqual(0.4f, timeScale.Resolve(TimeLayer.None), 1e-5f);
    }

    [Test]
    public void Events_ReportTargetLayers()
    {
        TimeScaleModifierHandle handle = timeScale.AddModifier(0.3f, TimeLayer.Global, TimeLayer.Player);
        timeScale.RemoveModifier(handle);

        Assert.AreEqual(2, listener.Changes.Count);
        Assert.AreEqual(TimeLayer.Global, listener.Changes[0]);
        Assert.AreEqual(listener.Changes[0], listener.Changes[1]);
    }

    [Test]
    public void Reset_RemovesAll()
    {
        timeScale.AddModifier(0.1f);
        timeScale.AddModifier(0.2f, TimeLayer.NPC);
        timeScale.Reset();

        Assert.AreEqual(0, timeScale.Modifiers.Count);
        Assert.AreEqual(1f, timeScale.Resolve(TimeLayer.Global), 1e-5f);
    }

    [Test]
    public void InvalidValues_AreRejectedOrClamped()
    {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

        Assert.IsFalse(timeScale.AddModifier(float.NaN).IsValid);
        Assert.IsFalse(timeScale.AddModifier(float.PositiveInfinity).IsValid);
        Assert.AreEqual(1f, timeScale.GetWorldTimeScale(), 1e-5f);

        Assert.IsTrue(timeScale.AddModifier(-2f, TimeLayer.NPC).IsValid);
        Assert.AreEqual(0f, timeScale.Resolve(TimeLayer.Bots), 1e-5f);
    }

    [Test]
    public void LayerClock_StaysContinuous_WhenLayersChange()
    {
        var clocks = new Dictionary<TimeLayer, float> { [TimeLayer.Player] = 100f, [TimeLayer.Player | TimeLayer.Enemy] = 40f };
        TimeLayer layers = TimeLayer.Player;
        var clock = new TimeLayerClock(() => layers, mask => clocks[mask]);

        float started = clock.Now;
        Assert.AreEqual(100f, started, 1e-5f);

        // Маска сменилась — часы объекта продолжают со 100, а не прыгают на 40.
        layers = TimeLayer.Player | TimeLayer.Enemy;
        Assert.AreEqual(100f, clock.Now, 1e-5f);

        // Дальше идут ходом новых часов.
        clocks[TimeLayer.Player | TimeLayer.Enemy] = 43f;
        Assert.AreEqual(103f, clock.Now, 1e-5f);

        // И обратно — снова без скачка.
        clocks[TimeLayer.Player] = 200f;
        layers = TimeLayer.Player;
        Assert.AreEqual(103f, clock.Now, 1e-5f);
        clocks[TimeLayer.Player] = 201f;
        Assert.AreEqual(104f, clock.Now, 1e-5f);
    }

    private sealed class Listener : IOnPRTimeScaleChange
    {
        public readonly List<TimeLayer> Changes = new();

        public void OnPRTimeScaleChange(TimeLayer layers) => Changes.Add(layers);
    }
}
#endif
