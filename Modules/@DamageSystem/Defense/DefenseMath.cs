using System;
using UnityEngine;

/// <summary>
/// Расчёт брони и сопротивлений без сцены: чистые формулы, которыми пользуется <see cref="DamageDefenseComponent"/>.
/// </summary>
/// <remarks>
/// <para>
/// Сопротивление — процент по каждому типу урона. Потолок и нижняя граница — из <see cref="DefenseSettings"/>,
/// пробивание атакующего вычитается уже после потолка. Отрицательное сопротивление — уязвимость.
/// </para>
/// <para>
/// Удар нескольких типов бьёт в самое слабое место: берётся сопротивление, которое пропускает больше всего урона.
/// Броня закрывает удар, только если все его типы физические (<see cref="DefenseSettings.ArmorDamageTypes"/>).
/// </para>
/// </remarks>
public static class DefenseMath
{
    /// <summary>
    /// Типы урона, у которых есть своё сопротивление. Остальные флаги — пометки удара
    /// (<see cref="DamageType.AreaOfEffect"/>, <see cref="DamageType.Critical"/>, <see cref="DamageType.TimeBased"/>):
    /// на защиту они не влияют.
    /// </summary>
    public const DamageType Kinds =
        DamageType.Fall | DamageType.Bullet | DamageType.Fire | DamageType.Ice | DamageType.Electric |
        DamageType.Poison | DamageType.Radiation | DamageType.Explosion | DamageType.Acid | DamageType.Mental;

    /// <summary>
    /// Все типы урона удара без пометок.
    /// </summary>
    public static DamageType KindsOf(DamageType damageType) => damageType & Kinds;

    /// <summary>
    /// Доля урона, которую снимает броня: броня / (броня + K), не больше потолка.
    /// </summary>
    /// <param name="armor">Броня цели.</param>
    /// <param name="penetrationPercent">Пробивание атакующего: какой процент брони не учитывается (0–100).</param>
    /// <param name="armorConstant">Константа K: броня, равная ей, снимает половину урона.</param>
    /// <param name="maxReduction">Потолок снижения (0–1).</param>
    public static float ArmorReduction(float armor, float penetrationPercent, float armorConstant, float maxReduction)
    {
        float effective = Mathf.Max(0f, armor) * (1f - Mathf.Clamp01(penetrationPercent / 100f));
        if (effective <= 0f)
            return 0f;

        float reduction = effective / (effective + Mathf.Max(1f, armorConstant));
        return Mathf.Min(reduction, Mathf.Clamp01(maxReduction));
    }

    /// <summary>
    /// Сопротивление, которое действует на удар: в границах настроек, затем минус пробивание атакующего.
    /// </summary>
    /// <param name="resistancePercent">Сопротивление цели, %.</param>
    /// <param name="penetrationPercent">Пробивание атакующего, %; отрицательное не учитывается.</param>
    /// <param name="minPercent">Нижняя граница, %.</param>
    /// <param name="maxPercent">Потолок, %.</param>
    public static float EffectiveResistance(float resistancePercent, float penetrationPercent, float minPercent, float maxPercent)
    {
        float min = Mathf.Min(minPercent, maxPercent);
        float capped = Mathf.Clamp(resistancePercent, min, maxPercent);
        return Mathf.Max(min, capped - Mathf.Max(0f, penetrationPercent));
    }

    /// <summary>
    /// Множитель урона от сопротивления: 75% — ×0,25, −50% — ×1,5.
    /// </summary>
    public static float ResistanceMultiplier(float effectiveResistancePercent)
    {
        return Mathf.Max(0f, 1f - effectiveResistancePercent / 100f);
    }

    /// <summary>
    /// Гасит ли иммунитет удар целиком: все типы удара под иммунитетом. Удар нескольких типов бьёт в самое слабое место,
    /// поэтому огненный взрыв по цели с иммунитетом только к огню проходит. У удара без типа иммунитета нет.
    /// </summary>
    public static bool IsImmune(DamageType damageType, DamageType immunities)
    {
        DamageType kinds = KindsOf(damageType);
        return kinds != DamageType.Generic && (kinds & ~immunities) == 0;
    }

    /// <summary>
    /// Закрывает ли броня удар этого типа.
    /// </summary>
    public static bool IsArmorApplicable(DamageType damageType, DefenseSettings settings)
    {
        settings ??= DefenseSettings.Default;
        DamageType kinds = KindsOf(damageType);
        if (kinds == DamageType.Generic)
            return settings.ArmorAppliesToGeneric;

        return (kinds & ~settings.ArmorDamageTypes) == 0;
    }

    /// <summary>
    /// Полный расчёт защиты от удара.
    /// </summary>
    /// <param name="damageType">Тип удара, флаги.</param>
    /// <param name="resistanceOf">Сопротивление цели одному типу урона, % (до потолка).</param>
    /// <param name="immunities">Типы урона, к которым у цели иммунитет.</param>
    /// <param name="armor">Броня цели.</param>
    /// <param name="armorPenetration">Пробивание брони атакующего, %.</param>
    /// <param name="resistancePenetration">Пробивание сопротивлений атакующего, %.</param>
    /// <param name="settings">Формулы проекта; <c>null</c> — <see cref="DefenseSettings.Default"/>.</param>
    public static DefenseResult Calculate(
        DamageType damageType,
        Func<DamageType, float> resistanceOf,
        DamageType immunities,
        float armor,
        float armorPenetration,
        float resistancePenetration,
        DefenseSettings settings)
    {
        settings ??= DefenseSettings.Default;

        DamageType kinds = KindsOf(damageType);
        float resistanceMultiplier = 1f;
        DamageType weakest = DamageType.Generic;
        bool immune = false;

        if (kinds != DamageType.Generic)
        {
            resistanceMultiplier = float.NegativeInfinity;
            for (int bit = 1; bit <= (int)Kinds; bit <<= 1)
            {
                DamageType kind = (DamageType)bit;
                if ((kinds & kind) == 0)
                    continue;

                float multiplier = (immunities & kind) != 0
                    ? 0f
                    : ResistanceMultiplier(EffectiveResistance(
                        resistanceOf != null ? resistanceOf(kind) : 0f,
                        resistancePenetration,
                        settings.MinResistance,
                        settings.MaxResistance));

                // Удар бьёт в самое слабое место: берём тип, который пропускает больше урона.
                if (multiplier > resistanceMultiplier)
                {
                    resistanceMultiplier = multiplier;
                    weakest = kind;
                }
            }

            immune = IsImmune(damageType, immunities);
        }

        float armorReduction = IsArmorApplicable(damageType, settings)
            ? ArmorReduction(armor, armorPenetration, settings.ArmorConstant, settings.MaxArmorReduction)
            : 0f;

        return new DefenseResult(resistanceMultiplier, armorReduction, weakest, immune);
    }
}

/// <summary>
/// Итог расчёта защиты от одного удара.
/// </summary>
public readonly struct DefenseResult
{
    /// <summary>
    /// Множитель урона от сопротивления: 1 — без изменений, больше 1 — уязвимость, 0 — иммунитет.
    /// </summary>
    public float ResistanceMultiplier { get; }

    /// <summary>
    /// Доля урона, которую сняла броня (0–1).
    /// </summary>
    public float ArmorReduction { get; }

    /// <summary>
    /// Тип урона, по которому считалось сопротивление (самое слабое место); у удара без типа — <see cref="DamageType.Generic"/>.
    /// </summary>
    public DamageType WeakestKind { get; }

    /// <summary>
    /// Удар целиком погасил иммунитет.
    /// </summary>
    public bool IsImmune { get; }

    /// <summary>
    /// Итоговый множитель урона.
    /// </summary>
    public float Multiplier => ResistanceMultiplier * (1f - ArmorReduction);

    /// <summary>
    /// Создаёт итог расчёта.
    /// </summary>
    public DefenseResult(float resistanceMultiplier, float armorReduction, DamageType weakestKind, bool isImmune)
    {
        ResistanceMultiplier = resistanceMultiplier;
        ArmorReduction = armorReduction;
        WeakestKind = weakestKind;
        IsImmune = isImmune;
    }

    /// <summary>
    /// Защиты нет: урон проходит без изменений.
    /// </summary>
    public static DefenseResult None => new(1f, 0f, DamageType.Generic, false);
}
