using System;

/// <summary>
/// Задаёт место значения в списке набора.
/// </summary>
/// <remarks>
/// <para>
/// Без него порядок берётся из кода: поля идут так, как объявлены. Для набора в одном
/// файле этого достаточно, но у <c>partial</c>-набора части лежат в разных файлах,
/// и порядок между ними определяется тем, в каком порядке компилятор получил файлы, —
/// то есть их именами. Атрибут убирает эту зависимость: порядок задан явно и переживает
/// и переименование файла, и добавление новой части в проекте.
/// </para>
/// <para>
/// Значения без атрибута считаются нулевыми и идут между отрицательными
/// и положительными, сохраняя между собой порядок объявления. Поэтому одному значению
/// можно назначить <c>-1</c>, чтобы поднять его наверх, не трогая остальные.
/// </para>
/// <para>
/// Порядок влияет и на выпадающий список в инспекторе, и на <c>FirstOption</c>,
/// а значит и на значение по умолчанию тех наборов, которые берут его оттуда.
/// </para>
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
