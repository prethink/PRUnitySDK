using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resource definition", menuName = "PRUnitySDK/Create/Definition/Resources")]
public class ResourceItemDefinition : ItemDefinitionBase
{
    [field: SerializeField] public EnumerationReference<ResourceEnumerationProvider> CurrencyType { get; private set; }
    [field: SerializeField] public AudioClip ResourceSound { get; protected set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public override string LocalizationKey => $"Resource_{Name}";

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => localization;

    public override string Id => CurrencyType.ToString();

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
