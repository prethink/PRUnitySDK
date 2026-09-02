using System;
using UnityEngine;

/// <summary>
/// Вспомогательные методы расчёта итоговых характеристик сущности.
/// </summary>
/// <remarks>
/// Модификаторы можно передать двумя способами: готовым сборщиком
/// <see cref="StatModifierCollector"/> либо делегатом — тогда считать стат можно
/// и без компонента на сцене, например в редакторе или в тестах.
/// </remarks>
public static class EntityStatsUtils
{
    /// <summary>
    /// Возвращает характеристику после финальных ограничений <see cref="GameRules"/>.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static float GetStat(
        Enumeration stat,
        EntityStatsBase entityStats,
        float defaultValue = 0f)
    {
        return GetStat(stat, entityStats, (Func<Enumeration, float, float>)null, defaultValue);
    }

    /// <summary>
    /// Возвращает характеристику после персональных модификаторов
    /// и финальных ограничений <see cref="GameRules"/>.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="modifier">Преобразование «ключ и базовое значение — итоговое значение» либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static float GetStat(
        Enumeration stat,
        EntityStatsBase entityStats,
        Func<Enumeration, float, float> modifier,
        float defaultValue = 0f)
    {
        if (stat == null)
            throw new ArgumentNullException(nameof(stat));

        float value = entityStats != null
            ? entityStats.Get(stat, defaultValue)
            : defaultValue;

        if (modifier != null)
            value = modifier(stat, value);

        return GameRules.ApplyStatRules(stat, value);
    }

    /// <summary>
    /// Возвращает целочисленную характеристику с округлением до ближайшего значения.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static int GetStatInt(
        Enumeration stat,
        EntityStatsBase entityStats,
        int defaultValue = 0)
    {
        return GetStatInt(stat, entityStats, (Func<Enumeration, float, float>)null, defaultValue);
    }

    /// <summary>
    /// Возвращает целочисленную характеристику с округлением до ближайшего значения.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="modifier">Преобразование «ключ и базовое значение — итоговое значение» либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static int GetStatInt(
        Enumeration stat,
        EntityStatsBase entityStats,
        Func<Enumeration, float, float> modifier,
        int defaultValue = 0)
    {
        return Mathf.RoundToInt(GetStat(stat, entityStats, modifier, defaultValue));
    }

    /// <summary>
    /// Возвращает характеристику типа <see cref="long"/> с округлением
    /// и защитой от выхода за границы типа.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static long GetStatLong(
        Enumeration stat,
        EntityStatsBase entityStats,
        long defaultValue = 0L)
    {
        return GetStatLong(stat, entityStats, (Func<Enumeration, float, float>)null, defaultValue);
    }

    /// <summary>
    /// Возвращает характеристику типа <see cref="long"/> с округлением
    /// и защитой от выхода за границы типа.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="modifier">Преобразование «ключ и базовое значение — итоговое значение» либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static long GetStatLong(
        Enumeration stat,
        EntityStatsBase entityStats,
        Func<Enumeration, float, float> modifier,
        long defaultValue = 0L)
    {
        double value = GetStat(stat, entityStats, modifier, defaultValue);

        if (double.IsNaN(value))
            return defaultValue;
        if (value <= long.MinValue)
            return long.MinValue;
        if (value >= long.MaxValue)
            return long.MaxValue;

        return (long)Math.Round(value);
    }

    /// <summary>
    /// Возвращает характеристику после персональных модификаторов сборщика.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static float GetStat(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector,
        float defaultValue = 0f)
    {
        return GetStat(stat, entityStats, ToModifier(collector), defaultValue);
    }

    /// <summary>
    /// Возвращает целочисленную характеристику после персональных модификаторов сборщика.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static int GetStatInt(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector,
        int defaultValue = 0)
    {
        return GetStatInt(stat, entityStats, ToModifier(collector), defaultValue);
    }

    /// <summary>
    /// Возвращает характеристику типа <see cref="long"/> после персональных модификаторов сборщика.
    /// </summary>
    /// <param name="stat">Ключ характеристики.</param>
    /// <param name="entityStats">Базовые характеристики сущности либо <c>null</c>.</param>
    /// <param name="collector">Сборщик персональных модификаторов либо <c>null</c>.</param>
    /// <param name="defaultValue">Значение при отсутствии характеристики.</param>
    public static long GetStatLong(
        Enumeration stat,
        EntityStatsBase entityStats,
        StatModifierCollector collector,
        long defaultValue = 0L)
    {
        return GetStatLong(stat, entityStats, ToModifier(collector), defaultValue);
    }

    /// <summary>
    /// Превращает сборщик в делегат модификатора.
    /// </summary>
    private static Func<Enumeration, float, float> ToModifier(StatModifierCollector collector)
    {
        return collector != null ? collector.ApplyStatModifier : null;
    }
}
