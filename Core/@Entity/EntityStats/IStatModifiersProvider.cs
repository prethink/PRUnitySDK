using System.Collections.Generic;

/// <summary>
/// Источник нескольких модификаторов характеристик.
/// </summary>
/// <remarks>
/// Реализуется компонентом на дочернем объекте сущности: шляпа, питомец, бафф.
/// Собирает их <see cref="StatModifierCollector"/>.
/// </remarks>
public interface IStatModifiersProvider
{
    /// <summary>
    /// Модификаторы, которые источник добавляет сущности.
    /// </summary>
    IEnumerable<StatModifier> StatModifiers { get; }
}
