using UnityEngine;

public static class EntityStatsUtils 
{
    public static float GetStat(Enumeration enumeration, EntityStatsBase entityStats, StatModifierCollector modifier = null, float defaultValue = 0)
    {
        var baseValue = entityStats.Get(enumeration, defaultValue);

        if (modifier != null)
            baseValue = modifier.ApplyStatModifier(enumeration, baseValue);
        return GameRules.ApplyStatRules(enumeration, baseValue);
    }

    public static int GetStatInt(Enumeration enumeration, EntityStatsBase entityStats, StatModifierCollector modifier = null, float defaultValue = 0)
    {
        return Mathf.RoundToInt(GetStat(enumeration, entityStats, modifier, defaultValue));
    }
}
