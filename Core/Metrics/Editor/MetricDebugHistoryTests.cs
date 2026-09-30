#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;

public class MetricDebugHistoryTests
{
    private bool originalCapture;

    [SetUp]
    public void SetUp()
    {
        originalCapture = MetricDebugHistory.CaptureEnabled;
        MetricDebugHistory.CaptureEnabled = true;
        MetricDebugHistory.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        MetricDebugHistory.Clear();
        MetricDebugHistory.CaptureEnabled = originalCapture;
    }

    [Test]
    public void Overloads_RecordOneCompletedCallEach()
    {
        var metric = new RecordingMetric();
        metric.Send("plain");
        metric.Send("path", "level", "5");
        metric.Send("legacy", new Dictionary<string, string> { { "mode", "race" } });
        MetricDebugEntry[] entries = MetricDebugHistory.Snapshot();
        Assert.AreEqual(3, metric.Calls);
        Assert.AreEqual(3, entries.Length);
        Assert.AreEqual("{}", entries[0].ParametersJson);
        Assert.AreEqual("5", (string)JObject.Parse(entries[1].ParametersJson)["level"]);
        Assert.AreEqual("race", (string)JObject.Parse(entries[2].ParametersJson)["mode"]);
        Assert.IsTrue(entries.All(entry => !entry.Ignored));
    }

    [Test]
    public void Snapshot_FreezesNestedParametersAndDoesNotShareTheHistoryArray()
    {
        var nested = new Dictionary<string, object> { { "level", "5" } };
        var list = new List<object> { 1, 2 };
        var parameters = new Dictionary<string, object> { { "user", nested }, { "scores", list } };
        var metric = new RecordingMetric();
        metric.Send("progress", parameters);
        nested["level"] = "9";
        list.Add(3);
        parameters.Clear();
        MetricDebugEntry[] entries = MetricDebugHistory.Snapshot();
        JObject json = JObject.Parse(entries[0].ParametersJson);
        Assert.AreEqual("5", (string)json["user"]["level"]);
        Assert.AreEqual(2, ((JArray)json["scores"]).Count);
        entries[0] = null;
        Assert.IsNotNull(MetricDebugHistory.Snapshot()[0]);
    }

    [Test]
    public void Capacity_EvictsOldestAndKeepsSequenceAndUtcTime()
    {
        var metric = new RecordingMetric();
        for (int index = 0; index <= MetricDebugHistory.Capacity; index++)
            metric.Send("event_" + index);
        MetricDebugEntry[] entries = MetricDebugHistory.Snapshot();
        Assert.AreEqual(MetricDebugHistory.Capacity, entries.Length);
        Assert.AreEqual("event_1", entries[0].EventName);
        Assert.AreEqual("event_" + MetricDebugHistory.Capacity, entries[entries.Length - 1].EventName);
        for (int index = 0; index < entries.Length; index++)
        {
            Assert.AreEqual(DateTimeKind.Utc, entries[index].TimestampUtc.Kind);
            if (index > 0)
                Assert.Greater(entries[index].Sequence, entries[index - 1].Sequence);
        }
    }

    [Test]
    public void CaptureOff_DoesNotChangeSendingAndClearDoesNotDisableCapture()
    {
        var metric = new RecordingMetric();
        MetricDebugHistory.CaptureEnabled = false;
        metric.Send("unrecorded");
        Assert.AreEqual(1, metric.Calls);
        Assert.IsEmpty(MetricDebugHistory.Snapshot());
        MetricDebugHistory.CaptureEnabled = true;
        metric.Send("recorded");
        MetricDebugHistory.Clear();
        Assert.IsTrue(MetricDebugHistory.CaptureEnabled);
        metric.Send("after_clear");
        Assert.AreEqual(1, MetricDebugHistory.Snapshot().Length);
    }

    [Test]
    public void DummyCalls_AreMarkedIgnored()
    {
        var metric = new DummyMetric();
        metric.Send("plain");
        metric.Send("tree", new Dictionary<string, object> { { "count", 2 } });
        MetricDebugEntry[] entries = MetricDebugHistory.Snapshot();
        Assert.AreEqual(2, entries.Length);
        Assert.IsTrue(entries.All(entry => entry.Ignored && entry.Adapter == typeof(DummyMetric).FullName));
    }

    [Test]
    public void CapturingUnreadableParameters_DoesNotBreakSuccessfulSend()
    {
        var metric = new RecordingMetric();
        Assert.DoesNotThrow(() => metric.Send("bad", new Dictionary<string, object> { { "value", new UnreadableValue() } }));
        Assert.AreEqual(1, metric.Calls);
        Assert.AreEqual(1, MetricDebugHistory.Snapshot().Length);
        StringAssert.Contains("could not be captured", MetricDebugHistory.Snapshot()[0].ParametersJson);
    }

    [Test]
    public void Tab_IsDiscoveredAndCanShowHistoryAfterPlayMode()
    {
        Assert.Contains(typeof(MetricsDebugTab), TypeCache.GetTypesDerivedFrom<IPRDebugTab>().ToArray());
        new RecordingMetric().Send("menu_click");
        var tab = new MetricsDebugTab();
        tab.Refresh(new PRDebugTabContext(string.Empty, 0, false));
        Assert.IsTrue(tab.AvailableInEditMode);
        Assert.AreEqual("Metrics (1)", tab.Title);
    }

    private sealed class RecordingMetric : MetricBase
    {
        public int Calls;
        public override void Send(string eventName)
        {
            Calls++;
            ReportSend(eventName);
        }
        public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams)
        {
            Calls++;
            ReportSend(eventName, eventParams);
        }
    }

    private sealed class UnreadableValue
    {
        public string Value => throw new InvalidOperationException("Cannot read parameters");
    }
}
#endif
