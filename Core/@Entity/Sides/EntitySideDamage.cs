/// <summary>
/// Что происходит с ударом одной стороны по другой.
/// </summary>
public enum EntitySideDamage
{
    /// <summary>
    /// Обычный удар.
    /// </summary>
    Hit = 0,

    /// <summary>
    /// Удар без урона: попадание засчитано, эффекты и звук играют, цель цела.
    /// </summary>
    NoDamage = 1,

    /// <summary>
    /// Удар отклонён (<see cref="DamageResult.Blocked"/>): эффекты урона не играют.
    /// </summary>
    Block = 2,
}
