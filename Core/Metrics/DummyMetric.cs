using System.Collections.Generic;

public class DummyMetric : MetricBase
{
    #region Базовый класс

    public override void Send(string eventName) { }

    public override void Send(string eventName, Dictionary<string, string> eventParams) { }

    public override void Send(string rootKeyEvent, string subKeyEvent, string subValueEvent) { }

    #endregion
}
