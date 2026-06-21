using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Игровые проекты.
/// </summary>
public static class GameRules
{
    /// <summary>
    /// Набор правил.
    /// </summary>
    private static readonly List<StatRuleBase> stateRules = new();
   
    /// <summary>
    /// Инициализация.
    /// </summary>
    public static void Initialize()
    {
        stateRules.Clear();

        var providers = ReflectionExtension.FindClassesImplementingInterface<IStatRuleProvider>();
        foreach (var provider in providers)
        {
            var instance = Activator.CreateInstance(provider) as IStatRuleProvider;
            PRLog.WriteDebug(typeof(GameRules), $"Initialize rule <color={Color.yellow}>{instance.RuleName}</color>");
            stateRules.AddRange(instance.GetRules());
        }
    }

    /// <summary>
    /// Применяет все правила, относящиеся к указанной характеристике, к текущему значению.
    /// </summary>
    public static float ApplyStatRules(Enumeration stat, float currentValue)
    {
        var currentRules = stateRules.Where(x => x.Stat == stat);
        if(!currentRules.Any())
            return currentValue;

        foreach (var rule in currentRules)
            currentValue = rule.Apply(currentValue);

        return currentValue;
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