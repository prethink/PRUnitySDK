using System;

/// <summary>
/// Контейнер изменяемых характеристик типа <see cref="long"/>.
/// </summary>
public sealed class LongPropertyContainer : NumericPropertyContainerBase<long>
{
    protected override long AddValues(long left, long right) =>
        ((decimal)left + right).ClampToLong(long.MinValue, long.MaxValue);

    protected override long MultiplyValues(long left, long right) =>
        ((decimal)left * right).ClampToLong(long.MinValue, long.MaxValue);

    protected override long ApplyGameRules(Enumeration key, long value) =>
        GameRules.ApplyLongStatRule(key, value);

    protected override long Zero => 0L;

    protected override long One => 1L;
}
