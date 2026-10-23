using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Глобальные ограничения характеристик.
/// Применяются последними - после базовых значений сущности и персональных модификаторов.
/// </summary>
/// <remarks>
/// Правила берутся из настроек активного проекта SDK (<see cref="PRSDKSettings.StatRules"/>): у каждой игры
/// свой набор, и правила одной игры на другую не накладываются.
/// </remarks>
public static class GameRules
{
    /// <summary>
    /// Правила по характеристикам, отсортированные по <see cref="StatRuleBase.Priority"/>.
    /// </summary>
    private static readonly Dictionary<Enumeration, List<StatRuleBase>> statRules = new();

    /// <summary>
    /// Пустой набор для характеристик без правил: возвращается наружу вместо null
    /// и не создаёт мусор на каждый запрос.
    /// </summary>
    private static readonly IReadOnlyList<StatRuleBase> emptyRules = Array.Empty<StatRuleBase>();

    /// <summary>
    /// Правила загружены хотя бы один раз.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Количество загруженных правил.
    /// </summary>
    public static int RuleCount { get; private set; }

    /// <summary>
    /// Характеристики, для которых задано хотя бы одно правило.
    /// </summary>
    public static IEnumerable<Enumeration> Stats => statRules.Keys;

    /// <summary>
    /// Загружает правила из настроек активного проекта.
    /// </summary>
    /// <remarks>
    /// Вызывается при старте SDK. В редакторе окно правил зовёт его повторно, если правила поменяли во время игры.
    /// </remarks>
    public static void Initialize()
    {
        PRSDKSettings settings = PRUnitySDK.Settings;
        if (settings == null)
            PRLog.WriteWarning(typeof(GameRules), "PRSDKSettings not found: stat rules are empty.");

        Initialize(settings != null ? settings.StatRules?.Rules : null);
    }

    /// <summary>
    /// Загружает переданный набор правил вместо текущего.
    /// </summary>
    /// <remarks>
    /// Пустой элемент или правило без характеристики пропускается с записью в лог: инициализация SDK
    /// из-за недозаполненного списка в настройках не падает.
    /// </remarks>
    public static void Initialize(IEnumerable<StatRuleBase> rules)
    {
        statRules.Clear();
        RuleCount = 0;

        if (rules != null)
        {
            int index = 0;
            foreach (StatRuleBase rule in rules)
            {
                Add(rule, index);
                index++;
            }
        }

        SortRules();
        IsInitialized = true;

        PRLog.WriteDebug(typeof(GameRules), $"Initialize Rules complete: {RuleCount} rules for {statRules.Count} stats.");
    }

    /// <summary>
    /// Применяет все правила, относящиеся к указанной характеристике, к текущему значению.
    /// </summary>
    public static float ApplyStatRules(Enumeration stat, float currentValue)
    {
        if (stat == null || !statRules.TryGetValue(stat, out List<StatRuleBase> currentRules))
            return currentValue;

        foreach (StatRuleBase rule in currentRules)
        {
            try
            {
                currentValue = rule.Apply(currentValue);
            }
            catch (Exception exception)
            {
                // Правила вызываются на каждый запрос характеристики, поэтому исключение
                // здесь ушло бы в геймплейный код. Значение остаётся тем, что было.
                PRLog.WriteError(typeof(GameRules),
                    $"Rule {rule.GetType().Name} for stat '{stat}' failed. {exception}");
            }
        }

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

    /// <summary>
    /// Возвращает правила указанной характеристики в порядке применения.
    /// </summary>
    public static IReadOnlyList<StatRuleBase> GetRules(Enumeration stat)
    {
        if (stat == null || !statRules.TryGetValue(stat, out List<StatRuleBase> rules))
            return emptyRules;

        return rules;
    }

    private static void Add(StatRuleBase rule, int index)
    {
        if (rule == null)
        {
            PRLog.WriteWarning(typeof(GameRules), $"Stat rule #{index + 1} is empty and is ignored.");
            return;
        }

        Enumeration stat = rule.Stat;
        if (stat == null)
        {
            PRLog.WriteWarning(typeof(GameRules),
                $"Rule #{index + 1} ({rule.GetType().Name}) has no stat and is ignored.");
            return;
        }

        if (!statRules.TryGetValue(stat, out List<StatRuleBase> statList))
        {
            statList = new List<StatRuleBase>();
            statRules.Add(stat, statList);
        }

        statList.Add(rule);
        RuleCount++;
    }

    /// <summary>
    /// Упорядочивает правила каждой характеристики по приоритету.
    /// </summary>
    /// <remarks>
    /// Меньшее значение <see cref="StatRuleBase.Priority"/> применяется раньше - как у
    /// <c>MethodHookAttribute.Order</c>. Для Min и Max порядок незаметен, но правило
    /// с некоммутативной операцией без сортировки зависело бы от порядка строк в настройках
    /// сильнее, чем от приоритета. Правила с равным приоритетом сохраняют порядок списка.
    /// </remarks>
    private static void SortRules()
    {
        foreach (List<StatRuleBase> rules in statRules.Values)
        {
            if (rules.Count > 1)
                StableSortByPriority(rules);
        }
    }

    /// <summary>
    /// Устойчивая сортировка по приоритету: <c>List.Sort</c> порядок равных не сохраняет.
    /// </summary>
    private static void StableSortByPriority(List<StatRuleBase> rules)
    {
        List<StatRuleBase> sorted = rules
            .Select((rule, index) => (rule, index))
            .OrderBy(entry => entry.rule.Priority)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.rule)
            .ToList();

        rules.Clear();
        rules.AddRange(sorted);
    }
}
