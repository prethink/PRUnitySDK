using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Игровые проекты.
/// </summary>
public static class GameRules
{
    /// <summary>
    /// Набор правил.
    /// </summary>
    private static readonly Dictionary<Enumeration, List<StatRuleBase>> statRules = new();

    // IStatRuleProvider lives in Assembly-CSharp, so its implementations cannot
    // live in an asmdef assembly (Unity asmdefs cannot reference Assembly-CSharp).
    // Scanning this one assembly avoids walking every Unity/package assembly.
    private static Type[] providerTypes;
   
    /// <summary>
    /// Инициализация.
    /// </summary>
    public static void Initialize()
    {
        statRules.Clear();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        providerTypes ??= FindProviderTypes();

        foreach (var providerType in providerTypes)
        {
            var instance = (IStatRuleProvider)Activator.CreateInstance(providerType);
            PRLog.WriteDebug(typeof(GameRules), $"Initialize rule <color={Color.yellow}>{instance.RuleName}</color>");

            foreach (var rule in instance.GetRules())
            {
                if (!statRules.TryGetValue(rule.Stat, out var rules))
                {
                    rules = new List<StatRuleBase>();
                    statRules.Add(rule.Stat, rules);
                }

                rules.Add(rule);
            }
        }
        stopwatch.Stop();
        PRLog.WriteDebug(typeof(GameRules), $"Initialize Rules complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    /// <summary>
    /// Применяет все правила, относящиеся к указанной характеристике, к текущему значению.
    /// </summary>
    public static float ApplyStatRules(Enumeration stat, float currentValue)
    {
        if (stat == null || !statRules.TryGetValue(stat, out var currentRules))
            return currentValue;

        foreach (var rule in currentRules)
            currentValue = rule.Apply(currentValue);

        return currentValue;
    }

    private static Type[] FindProviderTypes()
    {
        var interfaceType = typeof(IStatRuleProvider);
        var assemblyTypes = interfaceType.Assembly.GetTypes();
        var result = new List<Type>();

        foreach (var type in assemblyTypes)
        {
            if (!type.IsInterface && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
                result.Add(type);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Применяет правила к характеристике и округляет результат до типа long.
    /// </summary>
    public static long ApplyLongStatRule(Enumeration stat, float currentValue)
    {
        return (long)Math.Round(ApplyStatRules(stat, currentValue));
    }

    /// <summary>
    /// Применяет правила к характеристике и округляет результат до типа int.
    /// </summary>
    public static int ApplyIntStatRule(Enumeration stat, float currentValue)
    {
        return (int)Math.Round(ApplyStatRules(stat, currentValue));
    }
}
