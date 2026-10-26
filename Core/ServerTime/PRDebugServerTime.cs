using System;

/// <summary>
/// Часы площадки, ушедшие вперёд на ускорение времени для проверки (<see cref="PRDebugTime.Offset"/>).
/// </summary>
/// <remarks>
/// Обёртка над настоящими часами (локальными, Яндекса, GamePush): без ускорения сдвиг нулевой и время то же.
/// Ставится только в редакторе и отладочной сборке.
/// </remarks>
public sealed class PRDebugServerTime : IServerTime
{
    private readonly IServerTime inner;

    public PRDebugServerTime(IServerTime inner) => this.inner = inner;

    /// <summary>
    /// Настоящие часы, которые оборачиваются.
    /// </summary>
    public IServerTime Inner => inner;

    public DateTime GetNow() => inner.GetNow() + PRDebugTime.Offset;
}
