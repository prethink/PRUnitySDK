/// <summary>
/// Контейнер изменяемых характеристик типа <see cref="float"/>.
/// </summary>
public sealed class FloatPropertyContainer : NumericPropertyContainerBase<float>
{
    protected override float AddValues(float left, float right) => left + right;

    protected override float MultiplyValues(float left, float right) => left * right;

    protected override float ApplyGameRules(Enumeration key, float value) =>
        GameRules.ApplyStatRules(key, value);

    protected override float Zero => 0f;

    protected override float One => 1f;
}
