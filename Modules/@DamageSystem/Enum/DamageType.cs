using System;

[Flags]
public enum DamageType
{
    /// <summary>
    /// ќбщий урон.
    /// </summary>
    Generic = 0,
    /// <summary>
    /// ”рон от падени€.
    /// </summary>
    Fall = 1 << 0, 
    /// <summary>
    /// ”рон от пули.
    /// </summary>
    Bullet = 1 << 1,
    /// <summary>
    /// ”рон от огн€.
    /// </summary>
    Fire = 1 << 2,
    /// <summary>
    /// ”рон от холода
    /// </summary>
    Ice = 1 << 3,
    /// <summary>
    /// Ёлектрический урон
    /// </summary>
    Electric = 1 << 4,
    /// <summary>
    /// ядовитый урон.
    /// </summary>
    Poison = 1 << 5,
    /// <summary>
    /// –адиаци€.
    /// </summary>
    Radiation = 1 << 6,
    /// <summary>
    /// ”рон от взрыва.
    /// </summary>
    Explosion = 1 << 7,
    /// <summary>
    ///  ислота.
    /// </summary>
    Acid = 1 << 8,
    /// <summary>
    /// ѕсихологический урон.
    /// </summary>
    Mental = 1 << 9,
    /// <summary>
    /// ”рон по области.
    /// </summary>
    AreaOfEffect = 1 << 10, 
    /// <summary>
    ///  ритический урон.
    /// </summary>
    Critical = 1 << 11,
    /// <summary>
    /// ¬ременный периодический урон.
    /// </summary>
    TimeBased = 1 << 12,

}