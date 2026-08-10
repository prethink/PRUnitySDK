/// <summary>
/// Глобальное событие убийства сущности уроном.
/// </summary>
public class EntityKillEventArgs : CombatEventBase
{
    /// <summary>
    /// Подробный результат смертельного урона.
    /// </summary>
    public DamageOutcome Outcome { get; }

    /// <summary>
    /// Снимок данных смертельного урона.
    /// </summary>
    public DamageData Damage => Outcome?.DamageData;

    /// <summary>
    /// Создаёт событие убийства без подробных данных урона.
    /// </summary>
    /// <param name="attacker">Сущность-убийца.</param>
    /// <param name="victim">Убитая сущность.</param>
    /// <param name="weapon">Использованное оружие.</param>
    public EntityKillEventArgs(IEntity attacker, IEntity victim, IWeapon weapon) 
        : base(attacker, victim, weapon)
    {
    }

    /// <summary>
    /// Создаёт событие убийства без оружия и подробных данных урона.
    /// </summary>
    /// <param name="attacker">Сущность-убийца.</param>
    /// <param name="victim">Убитая сущность.</param>
    public EntityKillEventArgs(IEntity attacker, IEntity victim) 
        : base(attacker, victim, null)
    {
        
    }

    /// <summary>
    /// Создаёт событие убийства с полным результатом урона.
    /// </summary>
    /// <param name="attacker">Сущность-убийца.</param>
    /// <param name="victim">Убитая сущность.</param>
    /// <param name="outcome">Результат смертельного урона.</param>
    /// <param name="weapon">Использованное оружие либо <c>null</c>.</param>
    public EntityKillEventArgs(IEntity attacker, IEntity victim, DamageOutcome outcome, IWeapon weapon)
        : base(attacker, victim, weapon)
    {
        Outcome = outcome;
    }

    /// <summary>
    /// Возвращает идентификатор категории события убийства.
    /// </summary>
    /// <returns>Путь категории Kill.</returns>
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Kill");
    }
}
