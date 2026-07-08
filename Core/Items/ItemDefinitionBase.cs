using System.Collections.Generic;
using UnityEngine;

public abstract partial class ItemDefinitionBase : ScriptableObject, IItemDefinition
{
    public abstract string Id { get; }
    [field: SerializeField] public string Name { get; protected set; }
    [field: SerializeField, SpritePreview(140)] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    public abstract CategoryPath Category { get; }

    public abstract string LocalizationKey { get; }

    public abstract IReadOnlyDictionary<LangType, string> LocalizationValues { get; }
}
