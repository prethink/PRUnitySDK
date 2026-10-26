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
        Assert.AreEqual(TimeLayer.Global & ~TimeLayer.Player, listener.Changes[0]);
        Assert.AreEqual(TimeLayer.None, listener.Changes[0] & TimeLayer.Player);
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

    private sealed class Listener : IOnPRTimeScaleChange
    {
        public readonly List<TimeLayer> Changes = new();

        public void OnPRTimeScaleChange(TimeLayer layers) => Changes.Add(layers);
    }
}
#endif
