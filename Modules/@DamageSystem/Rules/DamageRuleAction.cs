/// <summary>
/// Что правило урона делает с подходящим ударом.
/// </summary>
public enum DamageRuleAction
{
    /// <summary>
    /// Умножить урон. Удар засчитан, эффекты попадания играют; множитель ноль — цель цела.
    /// </summary>
    Multiply = 0,

    /// <summary>
    /// Отклонить удар: результат <see cref="DamageResult.Blocked"/>, эффекты урона не играют.
    /// </summary>
    Block = 1
}
