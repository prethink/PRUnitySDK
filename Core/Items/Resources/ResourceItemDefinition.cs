using UnityEngine;

[CreateAssetMenu(fileName = "Resource definition", menuName = "PRUnitySDK/Create/Definition/Resources/Resource")]
public class ResourceItemDefinition : ResourceItemDefinitionBase
{
    [field: SerializeField] public EnumerationReference<ResourceEnumerations> CurrencyType { get; private set; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// Идентификатор ресурса — ключ его типа. Именно <c>Value</c>: у ссылки нет своего
    /// <c>ToString</c>, и он отдал бы имя класса, одинаковое у всех ресурсов.
    /// </remarks>
    public override string Id => EnumerationReference<ResourceEnumerations>.ToValue(CurrencyType);

    /// <summary>
    /// Пытается получить runtime-тип ресурса из сериализованной ссылки definition.
    /// </summary>
    /// <param name="resourceType">Настроенный тип ресурса или null.</param>
    /// <returns>true, если CurrencyType настроен корректно.</returns>
    public bool TryGetResourceType(out Enumeration resourceType)
    {
        resourceType = CurrencyType?.ToEnumeration();
        return resourceType != null;
    }
}
