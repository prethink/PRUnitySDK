/// <summary>
/// Ключи характеристик защиты: броня, сопротивления по типам урона и пробивание у атакующего.
/// </summary>
/// <remarks>
/// Значения читает <see cref="DamageDefenseComponent"/> через <see cref="StatModifierCollector"/> сущности,
/// поэтому броню и сопротивления выдаёт всё, что умеет давать модификаторы: экипировка, питомец, набор,
/// бустер, способность. Игроку ключи доступны и в наборе <c>PlayerStatsEnumeration</c> — это те же объекты.
/// </remarks>
public class DefenseStatEnumerations : EnumerationProviderBase
{
    /// <summary>
    /// Броня: снижает физический урон с убыванием, см. <see cref="DefenseMath.ArmorReduction"/>.
    /// </summary>
    public static readonly Enumeration Armor = new(nameof(Armor));

    /// <summary>
    /// Сопротивление всем типам урона, в процентах. Прибавляется к сопротивлению каждого типа.
    /// </summary>
    public static readonly Enumeration AllResistance = new(nameof(AllResistance));

    /// <summary>
    /// Сопротивление падению, в процентах.
    /// </summary>
    public static readonly Enumeration FallResistance = new(nameof(FallResistance));

    /// <summary>
    /// Сопротивление пулям, в процентах.
    /// </summary>
    public static readonly Enumeration BulletResistance = new(nameof(BulletResistance));

    /// <summary>
    /// Сопротивление огню, в процентах.
    /// </summary>
    public static readonly Enumeration FireResistance = new(nameof(FireResistance));

    /// <summary>
    /// Сопротивление холоду, в процентах.
    /// </summary>
    public static readonly Enumeration IceResistance = new(nameof(IceResistance));

    /// <summary>
    /// Сопротивление электричеству, в процентах.
    /// </summary>
    public static readonly Enumeration ElectricResistance = new(nameof(ElectricResistance));

    /// <summary>
    /// Сопротивление яду, в процентах.
    /// </summary>
    public static readonly Enumeration PoisonResistance = new(nameof(PoisonResistance));

    /// <summary>
    /// Сопротивление радиации, в процентах.
    /// </summary>
    public static readonly Enumeration RadiationResistance = new(nameof(RadiationResistance));

    /// <summary>
    /// Сопротивление взрывам, в процентах.
    /// </summary>
    public static readonly Enumeration ExplosionResistance = new(nameof(ExplosionResistance));

    /// <summary>
    /// Сопротивление кислоте, в процентах.
    /// </summary>
    public static readonly Enumeration AcidResistance = new(nameof(AcidResistance));

    /// <summary>
    /// Сопротивление урону по рассудку, в процентах.
    /// </summary>
    public static readonly Enumeration MentalResistance = new(nameof(MentalResistance));

    /// <summary>
    /// Пробивание брони у атакующего: какой процент брони цели его удары не замечают (0–100).
    /// </summary>
    public static readonly Enumeration ArmorPenetration = new(nameof(ArmorPenetration));

    /// <summary>
    /// Пробивание сопротивлений у атакующего: сколько процентов сопротивления цели снимают его удары.
    /// Действует после потолка, поэтому пробивает и тех, у кого сопротивление на максимуме.
    /// </summary>
    public static readonly Enumeration ResistancePenetration = new(nameof(ResistancePenetration));

    /// <inheritdoc />
    public override bool IncludeInherited => false;

    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    /// <summary>
    /// Ключ сопротивления для одного типа урона; <c>null</c>, если у типа сопротивления нет
    /// (<see cref="DamageType.Generic"/>, пометки <see cref="DamageType.Critical"/> и подобные).
    /// </summary>
    /// <param name="kind">Ровно один флаг из <see cref="DefenseMath.Kinds"/>.</param>
    public static Enumeration ResistanceFor(DamageType kind)
    {
        return kind switch
        {
            DamageType.Fall => FallResistance,
            DamageType.Bullet => BulletResistance,
            DamageType.Fire => FireResistance,
            DamageType.Ice => IceResistance,
            DamageType.Electric => ElectricResistance,
            DamageType.Poison => PoisonResistance,
            DamageType.Radiation => RadiationResistance,
            DamageType.Explosion => ExplosionResistance,
            DamageType.Acid => AcidResistance,
            DamageType.Mental => MentalResistance,
            _ => null
        };
    }
}
