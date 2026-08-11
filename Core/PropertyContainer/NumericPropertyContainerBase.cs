/// <summary>
/// Базовый контейнер числовых характеристик с поддержкой Add, Multiply и Override.
/// </summary>
public abstract class NumericPropertyContainerBase<T> : PropertyContainerBase<T>
{
    /// <summary>
    /// Нулевое значение числового типа.
    /// </summary>
    protected abstract T Zero { get; }

    /// <summary>
    /// Единичное значение числового типа.
    /// </summary>
    protected abstract T One { get; }

    protected abstract T AddValues(T left, T right);

    protected abstract T MultiplyValues(T left, T right);

    protected override T CalculateModifiers(Enumeration key, T baseValue)
    {
        if (!modifiers.TryGetValue(key, out var sources))
            return baseValue;

        T additive = Zero;
        T multiplier = One;
        Modifier selectedOverride = null;

        foreach (ModifierSourceContainer source in sources.Values)
        {
            foreach (Modifier modifier in source.Modifiers)
            {
                if (modifier.Type == ModifierTypes.Add)
                {
                    additive = AddValues(additive, modifier.Value);
                }
                else if (modifier.Type == ModifierTypes.Multiply)
                {
                    multiplier = MultiplyValues(multiplier, modifier.Value);
                }
                else if (modifier.Type == ModifierTypes.Override && IsHigherPriority(modifier, selectedOverride))
                {
                    selectedOverride = modifier;
                }
            }
        }

        if (selectedOverride != null)
            return selectedOverride.Value;

        return MultiplyValues(AddValues(baseValue, additive), multiplier);
    }

    private static bool IsHigherPriority(Modifier candidate, Modifier current)
    {
        if (current == null)
            return true;
        if (candidate.Priority != current.Priority)
            return candidate.Priority > current.Priority;

        return candidate.Order > current.Order;
    }
}
