using UnityEngine;

/// <summary>
/// Переводимые названия характеристик защиты для окон: «Защита от огня: +20».
/// </summary>
/// <remarks>
/// Ключи защиты заводит SDK, поэтому и названия регистрирует он сам — до загрузки первой сцены.
/// Игра может заменить любое название своим через <see cref="StatLabels.Register"/>.
/// </remarks>
public static class DefenseLabels
{
    public static readonly ILocalizationProvider Armor =
        new LocalizationProvider("stat_armor", "Armor", "Броня", "Zırh");

    public static readonly ILocalizationProvider AllResistance =
        new LocalizationProvider("stat_all_resistance", "All resistances", "Все сопротивления", "Tüm dirençler");

    public static readonly ILocalizationProvider FallResistance =
        new LocalizationProvider("stat_fall_resistance", "Fall resistance", "Защита от падения", "Düşme direnci");

    public static readonly ILocalizationProvider BulletResistance =
        new LocalizationProvider("stat_bullet_resistance", "Bullet resistance", "Защита от пуль", "Mermi direnci");

    public static readonly ILocalizationProvider FireResistance =
        new LocalizationProvider("stat_fire_resistance", "Fire resistance", "Защита от огня", "Ateş direnci");

    public static readonly ILocalizationProvider IceResistance =
        new LocalizationProvider("stat_ice_resistance", "Cold resistance", "Защита от холода", "Soğuk direnci");

    public static readonly ILocalizationProvider ElectricResistance =
        new LocalizationProvider("stat_electric_resistance", "Lightning resistance", "Защита от молний", "Elektrik direnci");

    public static readonly ILocalizationProvider PoisonResistance =
        new LocalizationProvider("stat_poison_resistance", "Poison resistance", "Защита от яда", "Zehir direnci");

    public static readonly ILocalizationProvider RadiationResistance =
        new LocalizationProvider("stat_radiation_resistance", "Radiation resistance", "Защита от радиации", "Radyasyon direnci");

    public static readonly ILocalizationProvider ExplosionResistance =
        new LocalizationProvider("stat_explosion_resistance", "Explosion resistance", "Защита от взрывов", "Patlama direnci");

    public static readonly ILocalizationProvider AcidResistance =
        new LocalizationProvider("stat_acid_resistance", "Acid resistance", "Защита от кислоты", "Asit direnci");

    public static readonly ILocalizationProvider MentalResistance =
        new LocalizationProvider("stat_mental_resistance", "Mind resistance", "Защита разума", "Zihin direnci");

    public static readonly ILocalizationProvider ArmorPenetration =
        new LocalizationProvider("stat_armor_penetration", "Armor piercing", "Пробивание брони", "Zırh delme");

    public static readonly ILocalizationProvider ResistancePenetration =
        new LocalizationProvider("stat_resistance_penetration", "Resistance piercing", "Пробивание защиты", "Direnç delme");

    /// <summary>
    /// Регистрирует названия всех характеристик защиты в <see cref="StatLabels"/>.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Register()
    {
        StatLabels.Register(DefenseStatEnumerations.Armor, Armor);
        StatLabels.Register(DefenseStatEnumerations.AllResistance, AllResistance);
        StatLabels.Register(DefenseStatEnumerations.FallResistance, FallResistance);
        StatLabels.Register(DefenseStatEnumerations.BulletResistance, BulletResistance);
        StatLabels.Register(DefenseStatEnumerations.FireResistance, FireResistance);
        StatLabels.Register(DefenseStatEnumerations.IceResistance, IceResistance);
        StatLabels.Register(DefenseStatEnumerations.ElectricResistance, ElectricResistance);
        StatLabels.Register(DefenseStatEnumerations.PoisonResistance, PoisonResistance);
        StatLabels.Register(DefenseStatEnumerations.RadiationResistance, RadiationResistance);
        StatLabels.Register(DefenseStatEnumerations.ExplosionResistance, ExplosionResistance);
        StatLabels.Register(DefenseStatEnumerations.AcidResistance, AcidResistance);
        StatLabels.Register(DefenseStatEnumerations.MentalResistance, MentalResistance);
        StatLabels.Register(DefenseStatEnumerations.ArmorPenetration, ArmorPenetration);
        StatLabels.Register(DefenseStatEnumerations.ResistancePenetration, ResistancePenetration);
    }
}
