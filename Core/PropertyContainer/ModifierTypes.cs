/// <summary>
/// Поддерживаемые операции модификации числовых характеристик.
/// </summary>
public static class ModifierTypes
{
    public static readonly Enumeration Add = new(nameof(Add));
    public static readonly Enumeration Multiply = new(nameof(Multiply));
    public static readonly Enumeration Override = new(nameof(Override));
}
