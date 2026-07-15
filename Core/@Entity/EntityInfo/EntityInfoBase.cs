using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityInfoBase : ScriptableObject, IEntityInfo
{
    [field: SerializeField] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    public string LocalizationKey => $"EntityInfo_{Name.ToLower()}";

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
}
