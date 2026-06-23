using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resource definition", menuName = "PRUnitySDK/Create/Definition/Resources")]
public class ResourceItemDefinition : ItemDefinitionBase, ILocalizationProvider
{
    public override CategoryPath Category => new CategoryPath(ResourceItemCategory.GetCategory(Name));
    [field: SerializeField] public EnumerationReference<ResourceEnumerationProvider> CurrencyType { get; private set; }
    [field: SerializeField] public AudioClip ResourceSound { get; protected set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public string LocalizationKey => $"Resource_{Name}";

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;

    public override string Id => CurrencyType.ToString();
}
