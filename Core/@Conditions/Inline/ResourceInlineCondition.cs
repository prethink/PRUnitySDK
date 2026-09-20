using System;
using UnityEngine;

/// <summary>
/// Встроенная форма правила по ресурсу: сравнивает накопленное с числом.
/// </summary>
/// <remarks>
/// Для слота прямо в инспекторе владельца, когда порог уникален для этого объекта.
/// Когда один и тот же порог нужен нескольким — заводят ассет
/// <see cref="ResourceCondition"/> и ссылаются на него через <see cref="AssetCondition"/>.
/// <para>
/// Расчёт берётся у ассета статически: сравнение и работа с кошельком написаны один раз
/// и разъехаться между формами не могут.
/// </para>
/// </remarks>
[Serializable]
public class ResourceInlineCondition : ICondition
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
    public long Missing => ResourceCondition.GetMissing(resource, comparison, amount);

    /// <inheritdoc />
    public bool Evaluate()
    {
        return ResourceCondition.Evaluate(resource, comparison, amount);
    }
}
