using System;
using UnityEngine;

/// <summary>
/// Персональный модификатор характеристики сущности.
/// </summary>
/// <remarks>
/// Задаёт, что именно делать с базовым значением: прибавить, умножить или заменить.
/// Ключ характеристики возвращает <see cref="GetEnumeration"/>, поэтому источники
/// модификаторов не привязаны к конкретному набору характеристик.
/// </remarks>
[Serializable]
public abstract class StatModifier
{
    /// <summary>
    /// Величина модификатора.
    /// </summary>
    [field: SerializeField] public float Value { get; protected set; }

    /// <summary>
    /// Способ применения к базовому значению.
    /// </summary>
    [field: SerializeField] public StatModifierType Type { get; protected set; }

    /// <summary>
    /// Приоритет: у <see cref="StatModifierType.Override"/> большее значение сильнее.
    /// </summary>
    [field: SerializeField] public int Priority { get; protected set; }

    /// <summary>
    /// Конструктор для сериализации: значения приходят из инспектора.
    /// </summary>
    protected StatModifier()
    {
    }

    /// <summary>
    /// Создаёт модификатор в коде.
    /// </summary>
    /// <param name="value">Величина модификатора.</param>
    /// <param name="type">Способ применения к базовому значению.</param>
    /// <param name="priority">Приоритет среди <see cref="StatModifierType.Override"/>.</param>
    protected StatModifier(float value, StatModifierType type, int priority = 0)
    {
        Value = value;
        Type = type;
        Priority = priority;
    }

    /// <summary>
    /// Возвращает ключ характеристики, на которую действует модификатор.
    /// </summary>
    public abstract Enumeration GetEnumeration();
}

/// <summary>
/// Модификатор характеристики из конкретного набора ключей.
/// </summary>
/// <typeparam name="TEnum">Провайдер ключей характеристик.</typeparam>
[Serializable]
public class StatModifier<TEnum> : StatModifier
    where TEnum : IEnumerationProvider, new()
{
    /// <summary>
    /// Характеристика, на которую действует модификатор.
    /// </summary>
    [field: SerializeField] public EnumerationReference<TEnum> Property { get; protected set; } = new();

    /// <summary>
    /// Конструктор для сериализации: значения приходят из инспектора.
    /// </summary>
    public StatModifier()
    {
    }

    /// <summary>
    /// Создаёт модификатор в коде — для временных источников без объекта на сцене:
    /// бустера, VIP-статуса, способности.
    /// </summary>
    /// <param name="stat">Характеристика из набора <typeparamref name="TEnum"/>.</param>
    /// <param name="value">Величина модификатора.</param>
    /// <param name="type">Способ применения к базовому значению.</param>
    /// <param name="priority">Приоритет среди <see cref="StatModifierType.Override"/>.</param>
    /// <exception cref="ArgumentException">Характеристики нет в наборе <typeparamref name="TEnum"/>.</exception>
    public StatModifier(Enumeration stat, float value, StatModifierType type, int priority = 0)
        : base(value, type, priority)
    {
        Property = new EnumerationReference<TEnum>();
        Property.Set(stat);
    }

    public override Enumeration GetEnumeration()
    {
        return Property.ToEnumeration();
    }
}
