using System.Collections.Generic;
using UnityEngine;

public abstract class EntityInfoBase : ScriptableObject, IEntityInfo
{
    [field: SerializeField] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    [SerializeField] protected LocalizationArray localizations;

    public string LocalizationKey => $"EntityInfo_{Name.ToLower()}";

    public IReadOnlyList<string> LocalizationValues => localizations.Values;
}
