using System;

/// <summary>
/// Контейнер изменяемых характеристик типа <see cref="long"/>.
/// </summary>
public sealed class LongPropertyContainer : NumericPropertyContainerBase<long>
{
    protected override long AddValues(long left, long right) => Clamp((decimal)left + right);

    protected override long MultiplyValues(long left, long right) => Clamp((decimal)left * right);

    protected override long ApplyGameRules(Enumeration key, long value) =>
        GameRules.ApplyLongStatRule(key, value);

    protected override long Zero => 0L;

    protected override long One => 1L;

    private static long Clamp(decimal value)
    {
        if (value <= long.MinValue)
            return long.MinValue;
        if (value >= long.MaxValue)
            return long.MaxValue;

        return (long)value;
    }
}
