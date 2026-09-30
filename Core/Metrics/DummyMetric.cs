using System.Collections.Generic;

public class DummyMetric : MetricBase
{
    #region Базовый класс

    public override void Send(string eventName) => ReportSend(eventName, ignored: true);

    public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams) => ReportSend(eventName, eventParams, ignored: true);

    #endregion
}
