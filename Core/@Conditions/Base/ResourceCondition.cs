using System;
using UnityEngine;

/// <summary>
/// Условие-ассет: накопленный ресурс сравнивается с числом.
/// </summary>
/// <remarks>
/// Тем и заводят переиспользуемые пороги: «десять кубков» и «сто кубков» — два файла,
/// цели ссылаются на них, и правка в одном месте меняет всех сразу.
/// <para>
/// Поля лежат прямо в ассете, без обёрток: открыл файл — сразу видно ресурс, сравнение
/// и число.
/// </para>
/// <para>
/// Тот же расчёт доступен статически, и им пользуется встроенная форма
/// <see cref="ResourceInlineCondition"/>. Так правило живёт в одном месте: сравнение
/// и работа с кошельком написаны один раз и разъехаться не могут.
/// </para>
/// </remarks>
[CreateAssetMenu(fileName = "Resource condition", menuName = "PRUnitySDK/Conditions/Resource condition")]
public class ResourceCondition : ConditionBase
{
    [SerializeField, IconPreview]
    [Tooltip("Ресурс, который сравнивается. Пусто - условие никого не запирает.")]
    private ResourceItemDefinition resource;

    [SerializeField]
    [Tooltip("Как сравнивать накопленное с числом.")]
    private ConditionComparison comparison = ConditionComparison.GreaterOrEqual;

    [SerializeField, Min(0)]
    [Tooltip("Число, с которым сравнивается накопленное.")]
    private long amount;

    /// <summary>
    /// Ресурс, который сравнивается.
    /// </summary>
    public ResourceItemDefinition Resource => resource;

    /// <summary>
    /// Как сравнивается накопленное.
    /// </summary>
    public ConditionComparison Comparison => comparison;

    /// <summary>
    /// Число, с которым сравнивается накопленное.
    /// </summary>
    public long Amount => amount;

    /// <summary>
    /// Сколько ресурса не хватает до выполнения; ноль, когда условие выполнено.
    /// </summary>
    public long Missing => GetMissing(resource, comparison, amount);

    /// <inheritdoc />
    public override bool Evaluate(GameObject actor = null)
    {
        return Evaluate(resource, comparison, amount);
    }

    /// <summary>
    /// Сравнивает накопленный ресурс с числом.
    /// </summary>
    /// <remarks>
    /// Невыбранный ресурс условие не запирает: пустая ссылка — это недонастроенный слот,
    /// и превращать его в вечный запрет хуже, чем пропустить.
    /// <para>
    /// Нулевое число при этом законно и коротким путём не отсекается: «ресурса нет вовсе»
    /// записывается как <see cref="ConditionComparison.Equal"/> с нулём.
    /// </para>
    /// </remarks>
    /// <param name="resource">Ресурс; <c>null</c> означает «условия нет».</param>
    /// <param name="comparison">Как сравнивать.</param>
    /// <param name="amount">Число для сравнения.</param>
    public static bool Evaluate(ResourceItemDefinition resource, ConditionComparison comparison, long amount)
    {
        if (resource == null)
            return true;

        long balance = GetBalance(resource);

        return comparison switch
        {
            ConditionComparison.GreaterOrEqual => balance >= amount,
            ConditionComparison.Greater => balance > amount,
            ConditionComparison.Equal => balance == amount,
            ConditionComparison.NotEqual => balance != amount,
            ConditionComparison.LessOrEqual => balance <= amount,
            ConditionComparison.Less => balance < amount,
            _ => true
        };
    }

    /// <summary>
    /// Сколько ресурса не хватает до выполнения.
    /// </summary>
    /// <remarks>
    /// Интерфейсу нужен не отказ, а «нужно ещё сорок»: по этому числу собирают подпись
    /// на запертой цели.
    /// <para>
    /// Осмысленно только там, где ресурс копят, — при сравнениях «больше» и «больше либо
    /// равно». У остальных накопление к выполнению не ведёт, и число всегда ноль.
    /// </para>
    /// </remarks>
    public static long GetMissing(ResourceItemDefinition resource, ConditionComparison comparison, long amount)
    {
        if (resource == null || Evaluate(resource, comparison, amount))
            return 0L;

        long balance = GetBalance(resource);

        return comparison switch
        {
            ConditionComparison.GreaterOrEqual => Math.Max(0L, amount - balance),
            ConditionComparison.Greater => Math.Max(0L, amount + 1L - balance),
            _ => 0L
        };
    }

    /// <summary>
    /// Сколько ресурса у игрока сейчас.
    /// </summary>
    /// <remarks>
    /// Через <see cref="WalletService"/>, а не напрямую через менеджер ресурсов: тем же
    /// способом спрашивает магазин, и двух ответов на один вопрос в проекте быть не должно.
    /// <para>
    /// До готовности SDK кошелька ещё нет, и баланс считается нулевым. Условие спрашивают
    /// заново на каждый вопрос, поэтому ответ исправится сам.
    /// </para>
    /// </remarks>
    public static long GetBalance(ResourceItemDefinition resource)
    {
        if (resource == null || !resource.TryGetResourceType(out Enumeration resourceType))
            return 0L;

        if (PRUnitySDK.Managers == null || PRUnitySDK.Managers.Resource == null)
            return 0L;

        return WalletService.Instance.GetBalance(resourceType);
    }
}
