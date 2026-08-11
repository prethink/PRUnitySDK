using System;

/// <summary>
/// Контейнер изменяемых характеристик типа <see cref="int"/>.
/// </summary>
public sealed class IntPropertyContainer : NumericPropertyContainerBase<int>
{
    protected override int AddValues(int left, int right) => Clamp((long)left + right);

    protected override int MultiplyValues(int left, int right) => Clamp((long)left * right);

    protected override int ApplyGameRules(Enumeration key, int value) =>
        GameRules.ApplyIntStatRule(key, value);

    protected override int Zero => 0;

    protected override int One => 1;

    private static int Clamp(long value) => (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));
}
