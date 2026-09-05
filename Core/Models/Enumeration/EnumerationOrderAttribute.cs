using System;

/// <summary>
/// Задаёт место значения в списке набора.
/// </summary>
/// <remarks>
/// Без атрибута порядок берётся из кода, а у <c>partial</c>-набора он зависит от имён
/// файлов. Значения без атрибута считаются нулевыми, поэтому поднять одно значение
/// наверх можно, не расставляя номера остальным. Порядок влияет и на выпадающий список,
/// и на <c>FirstOption</c>.
/// </remarks>
/// <example>
/// <code>
/// public partial class LevelObjectGroups : ObjectStateGroupEnumerations
/// {
///     [EnumerationOrder(-10)] public static readonly Enumeration Crystals = new(nameof(Crystals));
///     [EnumerationOrder(10)]  public static readonly Enumeration Doors = new(nameof(Doors));
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public sealed class EnumerationOrderAttribute : Attribute
{
    /// <summary>
    /// Чем меньше, тем раньше в списке.
    /// </summary>
    public int Order { get; }

    public EnumerationOrderAttribute(int order)
    {
        Order = order;
    }
}
