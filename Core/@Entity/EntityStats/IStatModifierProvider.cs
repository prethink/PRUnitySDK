/// <summary>
/// Источник одного модификатора характеристики.
/// </summary>
/// <remarks>
/// Упрощённый вариант <see cref="IStatModifiersProvider"/> для источников,
/// у которых модификатор всегда один.
/// </remarks>
public interface IStatModifierProvider
{
    /// <summary>
    /// Модификатор, который источник добавляет сущности.
    /// </summary>
    StatModifier StatModifier { get; }
}
