/// <summary>
/// Глобальное событие успешно применённого урона.
/// </summary>
public class TakeDamageEvent : CombatEventBase
{
    /// <summary>
    /// Снимок итоговых данных применённого урона.
    /// </summary>
    public DamageData Damage { get; protected set; }

    /// <summary>
    /// Подробный результат обработки; может отсутствовать у устаревших конструкторов.
    /// </summary>
    public DamageOutcome Outcome { get; protected set; }

    /// <summary>
    /// Результат обработки урона.
    /// </summary>
    public DamageResult Result => Outcome?.Result ?? DamageResult.Damaged;

    /// <summary>
    /// Количество фактически снятого здоровья.
    /// </summary>
    public float AppliedDamage => Outcome?.AppliedDamage ?? Damage?.Damage ?? 0f;

    /// <summary>
    /// Создаёт событие из данных урона с указанием оружия.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="damage">Итоговые данные урона.</param>
    /// <param name="weapon">Использованное оружие.</param>
    public TakeDamageEvent(IEntity attacker, IEntity victim, DamageData damage, IWeapon weapon)
        : base(attacker, victim, weapon)
    {
        Damage = damage?.Clone();
    }

    /// <summary>
    /// Создаёт событие из данных урона без оружия.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="damage">Итоговые данные урона.</param>
    public TakeDamageEvent(IEntity attacker, IEntity victim, DamageData damage)
        : base(attacker, victim, null)
    {
        Damage = damage?.Clone();
    }

    /// <summary>
    /// Создаёт событие из подробного результата обработки.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="outcome">Подробный результат.</param>
    /// <param name="weapon">Использованное оружие либо <c>null</c>.</param>
    public TakeDamageEvent(IEntity attacker, IEntity victim, DamageOutcome outcome, IWeapon weapon)
        : base(attacker, victim, weapon)
    {
        Outcome = outcome;
        Damage = outcome?.DamageData;
    }

    /// <summary>
    /// Возвращает идентификатор категории события применённого урона.
    /// </summary>
    /// <returns>Путь категории TakeDamage.</returns>
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "TakeDamage");
    }
}
