using System.Collections.Generic;

public class DummyMetric : MetricBase
{
    #region Базовый класс

    public override void Send(string eventName) { }

    public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams) { }

    #endregion
}
