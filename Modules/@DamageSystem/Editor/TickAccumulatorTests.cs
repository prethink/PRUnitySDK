#if PRSDK_TESTS
using NUnit.Framework;

public class TickAccumulatorTests
{
    [Test]
    public void SteadyFrames_GiveTickPerInterval()
    {
        var ticks = new TickAccumulator(0.5f, 5);
        int total = 0;
        for (int i = 0; i < 30; i++)
            total += ticks.Add(0.1f);

        Assert.AreEqual(6, total);
    }

    [Test]
    public void EffectEndsBeforeLastFrameIsCounted_FinishGivesLastTick()
    {
        // Яд на 3 с, тик 0,5 с: статус снят в 30-м кадре раньше, чем корутина его посчитала.
        var ticks = new TickAccumulator(0.5f, 5);
        int total = 0;
        for (int i = 0; i < 29; i++)
            total += ticks.Add(0.1f);

        total += ticks.Finish(0.1f);

        Assert.AreEqual(6, total);
    }

    [Test]
    public void Finish_PartialIntervalIsNotATick()
    {
        var ticks = new TickAccumulator(0.5f, 5);
        ticks.Add(0.2f);

        Assert.AreEqual(0, ticks.Finish(0.1f));
        Assert.AreEqual(0f, ticks.Elapsed);
    }

    [Test]
    public void LongFrame_CatchesUpTicks()
    {
        var ticks = new TickAccumulator(0.5f, 5);

        Assert.AreEqual(3, ticks.Add(1.5f));
    }

    [Test]
    public void VeryLongFrame_CapsAndDropsBacklog()
    {
        var ticks = new TickAccumulator(0.5f, 5);

        Assert.AreEqual(5, ticks.Add(10f));
        Assert.LessOrEqual(ticks.Elapsed, 0.5f);
        Assert.AreEqual(1, ticks.Add(0f));
        Assert.AreEqual(0, ticks.Add(0f));
    }

    [Test]
    public void ZeroTime_NoTicks()
    {
        var ticks = new TickAccumulator(0.5f, 5);

        Assert.AreEqual(0, ticks.Add(0f));
        Assert.AreEqual(0, ticks.Add(-1f));
    }
}
#endif
