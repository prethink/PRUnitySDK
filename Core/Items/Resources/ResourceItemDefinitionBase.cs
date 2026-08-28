using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ResourceItemDefinitionBase : ItemDefinitionBase
{
    [field: SerializeField] public AudioClip ResourceSound { get; protected set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public override string LocalizationKey => $"Resource_{Name}";

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
}
