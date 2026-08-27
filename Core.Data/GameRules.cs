using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Глобальные ограничения характеристик.
/// Применяются последними - после базовых значений сущности и персональных модификаторов.
/// </summary>
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

    // IStatRuleProvider lives in Assembly-CSharp, so its implementations cannot
    // live in an asmdef assembly (Unity asmdefs cannot reference Assembly-CSharp).
    // Scanning this one assembly avoids walking every Unity/package assembly.
    private static Type[] providerTypes;

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
    /// Находит все реализации <see cref="IStatRuleProvider"/> и загружает их правила.
    /// </summary>
    /// <remarks>
    /// Ошибка отдельного провайдера не прерывает загрузку: он пропускается с записью
    /// в лог, а остальные наборы правил применяются. Инициализация SDK из-за чужого
    /// набора правил не падает.
    /// </remarks>
    public static void Initialize()
    {
        statRules.Clear();
        RuleCount = 0;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        providerTypes ??= FindProviderTypes();

        foreach (Type providerType in providerTypes)
        {
            IStatRuleProvider provider = CreateProvider(providerType);
            if (provider == null)
                continue;

            LoadRules(provider, providerType);
        }

        SortRules();

        stopwatch.Stop();
        IsInitialized = true;

        PRLog.WriteDebug(typeof(GameRules),
            $"Initialize Rules complete: {RuleCount} rules for {statRules.Count} stats in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
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
    /// <remarks>
    /// Нужно, чтобы можно было ответить на вопрос «почему значение обрезано»:
    /// правила меняют число молча, и без такого списка причину видно только в коде.
    /// </remarks>
    public static IReadOnlyList<StatRuleBase> GetRules(Enumeration stat)
    {
        if (stat == null || !statRules.TryGetValue(stat, out List<StatRuleBase> rules))
            return emptyRules;

        return rules;
    }

    /// <summary>
    /// Создаёт провайдер правил, изолируя ошибку его конструктора.
    /// </summary>
    private static IStatRuleProvider CreateProvider(Type providerType)
    {
        try
        {
            return (IStatRuleProvider)Activator.CreateInstance(providerType);
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(GameRules),
                $"Cannot create stat rule provider <color={Color.yellow}>{providerType.Name}</color>. {exception}");
            return null;
        }
    }

    /// <summary>
    /// Загружает правила одного провайдера в общий набор.
    /// </summary>
    private static void LoadRules(IStatRuleProvider provider, Type providerType)
    {
        IEnumerable<StatRuleBase> rules;
        try
        {
            rules = provider.GetRules();
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(GameRules),
                $"Stat rule provider <color={Color.yellow}>{providerType.Name}</color> failed to return rules. {exception}");
            return;
        }

        if (rules == null)
        {
            PRLog.WriteWarning(typeof(GameRules),
                $"Stat rule provider {providerType.Name} returned null.");
            return;
        }

        int added = 0;
        foreach (StatRuleBase rule in rules)
        {
            if (rule == null)
            {
                PRLog.WriteWarning(typeof(GameRules),
                    $"Stat rule provider {providerType.Name} returned a null rule.");
                continue;
            }

            if (rule.Stat == null)
            {
                PRLog.WriteWarning(typeof(GameRules),
                    $"Rule {rule.GetType().Name} from {providerType.Name} has no stat and is ignored.");
                continue;
            }

            if (!statRules.TryGetValue(rule.Stat, out List<StatRuleBase> statList))
            {
                statList = new List<StatRuleBase>();
                statRules.Add(rule.Stat, statList);
            }

            statList.Add(rule);
            added++;
        }

        RuleCount += added;

        PRLog.WriteDebug(typeof(GameRules),
            $"Initialize rule <color={Color.yellow}>{SafeName(provider, providerType)}</color> ({added} rules)");
    }

    /// <summary>
    /// Упорядочивает правила каждой характеристики по приоритету.
    /// </summary>
    /// <remarks>
    /// Меньшее значение <see cref="StatRuleBase.Priority"/> применяется раньше - как у
    /// <c>MethodHookAttribute.Order</c>. Без сортировки порядок определялся бы обходом
    /// <c>Assembly.GetTypes()</c>, который спецификацией не гарантирован; для Min и Max это
    /// незаметно, но любое правило с некоммутативной операцией давало бы плавающий результат.
    /// Правила с равным приоритетом сохраняют порядок объявления.
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

    private static string SafeName(IStatRuleProvider provider, Type providerType)
    {
        try
        {
            return string.IsNullOrWhiteSpace(provider.RuleName) ? providerType.Name : provider.RuleName;
        }
        catch
        {
            return providerType.Name;
        }
    }

    /// <summary>
    /// Собирает типы провайдеров, пригодные к созданию.
    /// </summary>
    private static Type[] FindProviderTypes()
    {
        Type interfaceType = typeof(IStatRuleProvider);

        Type[] assemblyTypes;
        try
        {
            assemblyTypes = interfaceType.Assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            // Часть типов сборки может не загрузиться; берём то, что доступно,
            // вместо того чтобы остаться совсем без правил.
            PRLog.WriteError(typeof(GameRules),
                $"Cannot scan assembly for stat rule providers. {exception}");
            assemblyTypes = exception.Types.Where(type => type != null).ToArray();
        }

        var result = new List<Type>();

        foreach (Type type in assemblyTypes)
        {
            if (type.IsInterface || type.IsAbstract || !interfaceType.IsAssignableFrom(type))
                continue;

            if (type.ContainsGenericParameters)
            {
                PRLog.WriteWarning(typeof(GameRules),
                    $"Stat rule provider {type.Name} is generic and cannot be created automatically.");
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                PRLog.WriteWarning(typeof(GameRules),
                    $"Stat rule provider {type.Name} has no public parameterless constructor.");
                continue;
            }

            result.Add(type);
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return result.ToArray();
    }
}
