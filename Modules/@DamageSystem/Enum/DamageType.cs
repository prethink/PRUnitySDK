using System;

/// <summary>
/// Вид урона. Флаги комбинируются: один удар может быть одновременно взрывным,
/// огненным и критическим. Броня и сопротивления (<see cref="DamageDefenseComponent"/>)
/// у такого удара бьют в самое слабое место: берётся сопротивление того типа, который
/// пропускает больше урона (<see cref="DefenseMath.Calculate"/>).
/// Пометки <see cref="AreaOfEffect"/>, <see cref="Critical"/> и <see cref="TimeBased"/>
/// типами урона не считаются и на защиту не влияют.
/// </summary>
[Flags]
public enum DamageType
{
    /// <summary>
    /// Вид не указан: обычный удар кулаком или оружием. Сопротивления у него нет, броня его закрывает
    /// (<see cref="DefenseSettings.ArmorAppliesToGeneric"/>).
    /// </summary>
    Generic = 0,

    /// <summary>
    /// Падение с высоты.
    /// </summary>
    Fall = 1 << 0,

    /// <summary>
    /// Огнестрельное попадание.
    /// </summary>
    Bullet = 1 << 1,

    /// <summary>
    /// Огонь и горение.
    /// </summary>
    Fire = 1 << 2,

    /// <summary>
    /// Холод и обморожение.
    /// </summary>
    Ice = 1 << 3,

    /// <summary>
    /// Электричество.
    /// </summary>
    Electric = 1 << 4,

    /// <summary>
    /// Яд.
    /// </summary>
    Poison = 1 << 5,

    /// <summary>
    /// Радиация.
    /// </summary>
    Radiation = 1 << 6,

    /// <summary>
    /// Взрыв.
    /// </summary>
    Explosion = 1 << 7,

    /// <summary>
    /// Кислота.
    /// </summary>
    Acid = 1 << 8,

    /// <summary>
    /// Урон по рассудку, не связанный с физическим воздействием.
    /// </summary>
    Mental = 1 << 9,

    /// <summary>
    /// Урон по площади: получен не прямым попаданием, а зоной поражения.
    /// Ставится дополнительно к виду урона, а не вместо него.
    /// </summary>
    AreaOfEffect = 1 << 10,

    /// <summary>
    /// Критическое попадание. Выставляется зональным декоратором и читается
    /// через <see cref="DamageOutcome.WasCritical"/>.
    /// </summary>
    Critical = 1 << 11,

    /// <summary>
    /// Периодический урон: тик эффекта, а не одиночное попадание.
    /// Ставится источником периодического урона: <see cref="DamageOverTimeCoroutine"/>
    /// проставляет его сам, наследникам <see cref="TickDamageBrain"/> нужно сделать это
    /// в своём уроне.
    /// </summary>
    TimeBased = 1 << 12,
}
