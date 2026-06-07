using System.Collections.Generic;
using UnityEngine;

public abstract partial class ItemDefinitionBase : ScriptableObject, IItemDefinition
{
    public abstract string Id { get; }
    [field: SerializeField] public string Name { get; protected set; }
    [field: SerializeField, SpritePreview(140)] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    public abstract CategoryPath Category { get; }

    [field: SerializeField] public LocalizationArray Localization { get; protected set; }

    public abstract string LocalizationKey { get; }

    public IReadOnlyList<string> LocalizationValues => Localization.Values;
}
