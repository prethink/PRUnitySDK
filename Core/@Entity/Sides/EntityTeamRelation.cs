/// <summary>
/// Как пара сущностей связана командами.
/// </summary>
public enum EntityTeamRelation
{
    /// <summary>
    /// Командами пара не решается: хотя бы одна сторона не игрок или без команды
    /// (<c>Default</c>). Решает матрица сторон.
    /// </summary>
    None = 0,

    /// <summary>
    /// Оба игрока в одной команде.
    /// </summary>
    SameTeam = 1,

    /// <summary>
    /// Игроки в разных командах.
    /// </summary>
    OtherTeam = 2,
}
