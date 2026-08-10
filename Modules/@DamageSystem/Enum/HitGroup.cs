/// <summary>
/// Зона сущности, в которую пришлось попадание.
/// </summary>
public enum HitGroup
{
    /// <summary>
    /// Зона не определена или неприменима.
    /// </summary>
    Generic,

    /// <summary>
    /// Голова.
    /// </summary>
    Head,

    /// <summary>
    /// Грудь.
    /// </summary>
    Chest,

    /// <summary>
    /// Живот.
    /// </summary>
    Stomach,

    /// <summary>
    /// Левая рука.
    /// </summary>
    LeftArm,

    /// <summary>
    /// Правая рука.
    /// </summary>
    RightArm,

    /// <summary>
    /// Левая нога.
    /// </summary>
    LeftLeg,

    /// <summary>
    /// Правая нога.
    /// </summary>
    RightLeg
}
