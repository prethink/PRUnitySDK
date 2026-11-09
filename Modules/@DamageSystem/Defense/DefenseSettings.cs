using System;
using UnityEngine;

/// <summary>
/// Формулы брони и сопротивлений проекта: константа брони, потолки и какой урон считается физическим.
/// </summary>
[Serializable]
[SettingsDescription("Броня и сопротивления (DamageDefenseComponent): константа брони, потолки снижения урона " +
                     "и какие типы урона закрывает броня.")]
public class DefenseSettings
{
    /// <summary>
    /// Настройки по умолчанию — когда настроек проекта нет (тесты, редактор без SDK).
    /// </summary>
    public static readonly DefenseSettings Default = new();

    /// <summary>
    /// Константа брони K: снижение урона = броня / (броня + K). Броня, равная K, снимает половину урона.
    /// </summary>
    [field: SerializeField, Min(1f)]
    [field: Tooltip("Броня, равная этому числу, снимает половину физического урона. Снижение = броня / (броня + K): " +
                    "каждая следующая единица брони чуть слабее предыдущей.")]
    public float ArmorConstant { get; private set; } = 100f;

    /// <summary>
    /// Наибольшая доля урона, которую снимает броня (0–1).
    /// </summary>
    [field: SerializeField, Range(0f, 1f)]
    [field: Tooltip("Наибольшая доля урона, которую может снять броня. 0,85 — как в Diablo: хотя бы 15% урона проходит всегда.")]
    public float MaxArmorReduction { get; private set; } = 0.85f;

    /// <summary>
    /// Потолок сопротивления, в процентах. Всё, что выше, не действует.
    /// </summary>
    [field: SerializeField, Range(0f, 100f)]
    [field: Tooltip("Потолок сопротивления, %. 75 — как в Diablo. Полная защита от типа урона — флаг иммунитета у компонента, " +
                    "а не сопротивление 100.")]
    public float MaxResistance { get; private set; } = 75f;

    /// <summary>
    /// Нижняя граница сопротивления, в процентах. Отрицательное сопротивление — уязвимость.
    /// </summary>
    [field: SerializeField, Range(-300f, 0f)]
    [field: Tooltip("Нижняя граница сопротивления, %. −100 — урон вдвое больше. Пробивание ниже не опускает.")]
    public float MinResistance { get; private set; } = -100f;

    /// <summary>
    /// Физические типы урона: их закрывает броня.
    /// </summary>
    /// <remarks>
    /// Броня действует, только если все типы удара физические. Огненный взрыв (<c>Fire | Explosion</c>)
    /// броня не закрывает: удар бьёт в самое слабое место.
    /// </remarks>
    [field: SerializeField]
    [field: Tooltip("Типы урона, которые закрывает броня. Удар с хотя бы одним типом не из списка (огненный взрыв) " +
                    "броню не замечает. Падение по умолчанию не включено: броня от него не спасает.")]
    public DamageType ArmorDamageTypes { get; private set; } = DamageType.Bullet | DamageType.Explosion;

    /// <summary>
    /// Закрывает ли броня удар без типа (<see cref="DamageType.Generic"/>): кулак, обычное оружие.
    /// </summary>
    [field: SerializeField]
    [field: Tooltip("Удар без типа (Generic) — кулак, меч, обычный удар NPC — тоже физический, и броня его закрывает.")]
    public bool ArmorAppliesToGeneric { get; private set; } = true;

    /// <summary>
    /// Настройки по умолчанию; значения приходят из инспектора.
    /// </summary>
    public DefenseSettings()
    {
    }

    /// <summary>
    /// Настройки с заданными числами — для тестов и расчётов в редакторе.
    /// </summary>
    public DefenseSettings(
        float armorConstant,
        float maxArmorReduction,
        float maxResistance,
        float minResistance,
        DamageType armorDamageTypes,
        bool armorAppliesToGeneric)
    {
        ArmorConstant = armorConstant;
        MaxArmorReduction = maxArmorReduction;
        MaxResistance = maxResistance;
        MinResistance = minResistance;
        ArmorDamageTypes = armorDamageTypes;
        ArmorAppliesToGeneric = armorAppliesToGeneric;
    }

    /// <summary>
    /// Настройки текущего проекта, а без них — <see cref="Default"/>.
    /// </summary>
    public static DefenseSettings Current => PRUnitySDK.Settings?.Defense ?? Default;
}
