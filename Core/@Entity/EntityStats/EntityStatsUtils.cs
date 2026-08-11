using System;
using UnityEngine;

/// <summary>
/// Вспомогательные методы расчёта итоговых характеристик сущности.
/// </summary>
public static class EntityStatsUtils
{
    /// <summary>
    /// Возвращает характеристику после персональных модификаторов
    /// и финальных ограничений <see cref="GameRules"/>.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static float GetStat(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector = null,
        float defaultValue = 0f)
    {
        if (stat == null)
            throw new ArgumentNullException(nameof(stat));

        float value = entityStats != null
            ? entityStats.Get(stat, defaultValue)
            : defaultValue;

        if (collector != null)
            value = collector.ApplyStatModifier(stat, value);

        return GameRules.ApplyStatRules(stat, value);
    }

    /// <summary>
    /// Возвращает целочисленную характеристику с округлением до ближайшего значения.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static int GetStatInt(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector = null,
        int defaultValue = 0)
    {
        return Mathf.RoundToInt(GetStat(stat, entityStats, collector, defaultValue));
    }

    /// <summary>
    /// Возвращает характеристику типа <see cref="long"/> с округлением
    /// и защитой от выхода за границы типа.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static long GetStatLong(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector = null,
        long defaultValue = 0L)
    {
        double value = GetStat(stat, entityStats, collector, defaultValue);

        if (double.IsNaN(value))
            return defaultValue;
        if (value <= long.MinValue)
            return long.MinValue;
        if (value >= long.MaxValue)
            return long.MaxValue;

        return (long)Math.Round(value);
    }
}
