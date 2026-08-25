using System;

/// <summary>
/// Вид урона. Флаги комбинируются: один удар может быть одновременно взрывным,
/// огненным и критическим, а <see cref="DamageResistanceComponent"/> сработает,
/// если совпал хотя бы один флаг правила.
/// </summary>
[Flags]
public enum DamageType
{
    /// <summary>
    /// Вид не указан. Совпадает только с правилами сопротивления, где тоже указан Generic.
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
    /// Ставится источником периодического урона - например, TickDamageBrain.
    /// </summary>
    TimeBased = 1 << 12,
}
