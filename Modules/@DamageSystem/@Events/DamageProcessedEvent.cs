/// <summary>
/// Глобальное событие завершения любой попытки нанесения урона.
/// </summary>
public sealed class DamageProcessedEvent : CombatEventBase
{
    /// <summary>
    /// Подробный результат завершённой попытки.
    /// </summary>
    public DamageOutcome Outcome { get; }

    /// <summary>
    /// Создаёт событие завершения обработки урона.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="outcome">Итог обработки.</param>
    /// <param name="weapon">Использованное оружие либо <c>null</c>.</param>
    public DamageProcessedEvent(
        IEntity attacker,
        IEntity victim,
        DamageOutcome outcome,
        IWeapon weapon)
        : base(attacker, victim, weapon)
    {
        Outcome = outcome;
    }

    /// <summary>
    /// Создаёт событие завершения обработки урона без оружия.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="outcome">Итог обработки.</param>
    public DamageProcessedEvent(IEntity attacker, IEntity victim, DamageOutcome outcome)
        : this(attacker, victim, outcome, null)
    {
    }

    /// <summary>
    /// Возвращает идентификатор категории завершённой обработки.
    /// </summary>
    /// <returns>Путь категории DamageProcessed.</returns>
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "DamageProcessed");
    }
}
