/// <summary>
/// Стороны сущностей: по ним матрица решает, кто кого бьёт.
/// </summary>
/// <remarks>
/// Набор нарочно короткий: это не виды сущностей, а «чья она». Проект дописывает свои
/// стороны partial-классом, как любой набор перечислений.
/// </remarks>
public partial class EntitySideEnumerations : EnumerationProviderBase
{
    /// <summary>
    /// Ничья: сторона по умолчанию для всего, что не сопоставлено.
    /// </summary>
    public static readonly Enumeration Neutral = new(nameof(Neutral));

    /// <summary>
    /// Живые игроки.
    /// </summary>
    public static readonly Enumeration Players = new(nameof(Players));

    /// <summary>
    /// Боты, играющие наравне с игроками.
    /// </summary>
    public static readonly Enumeration Bots = new(nameof(Bots));

    /// <summary>
    /// Персонажи мира, не участвующие в игре как игроки.
    /// </summary>
    public static readonly Enumeration Npc = new(nameof(Npc));

    /// <summary>
    /// Противники.
    /// </summary>
    public static readonly Enumeration Monsters = new(nameof(Monsters));

    /// <summary>
    /// То, что ломают: блоки, ящики, препятствия.
    /// </summary>
    public static readonly Enumeration Breakables = new(nameof(Breakables));

    /// <inheritdoc />
    public override bool IncludeInherited => true;

    /// <inheritdoc />
    public override Enumeration Default => Neutral;
}
