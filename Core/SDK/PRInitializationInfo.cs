using System;

/// <summary>
/// Диагностическая информация об успешно завершённом элементе инициализации PRUnitySDK.
/// </summary>
public sealed class PRInitializationInfo
{
    /// <summary>
    /// Категория элемента инициализации.
    /// </summary>
    public PRInitializationCategory Category { get; }

    /// <summary>
    /// Отображаемое имя элемента.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Тип контракта, через который элемент зарегистрирован в SDK.
    /// </summary>
    public Type ContractType { get; }

    /// <summary>
    /// Фактический тип созданной реализации.
    /// </summary>
    public Type ImplementationType { get; }

    /// <summary>
    /// Полное время операции в миллисекундах.
    /// </summary>
    public double DurationMilliseconds { get; }

    /// <summary>
    /// Создаёт снимок результата инициализации.
    /// </summary>
    public PRInitializationInfo(PRInitializationCategory category, string name, Type contractType,
        Type implementationType, double durationMilliseconds)
    {
        Category = category;
        Name = name;
        ContractType = contractType;
        ImplementationType = implementationType;
        DurationMilliseconds = durationMilliseconds;
    }
}
