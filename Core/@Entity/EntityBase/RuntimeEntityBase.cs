using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeEntityBase : EntityBase, IEntityMetadata, IEntityMetadataProvider
{
    [field: SerializeField] public Sprite Icon { get; protected set; }

    [field: SerializeField] public string LocalizationKey { get; protected set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    public IEntityMetadata EntityMetadata => Description.GetMetadata();

    protected IEntityMetadata baseEntityMetadata;
    protected IEntityMetadata overrideEntityMetadata;

    protected override void InitializeEntityMetadata()
    {
        baseEntityMetadata = this;
        overrideEntityMetadata = this.GetComponent<IEntityMetadataProvider>()?.EntityMetadata;

        if (baseEntityMetadata != null && overrideEntityMetadata != null)
        {
            Description = new EntityDescription(baseEntityMetadata, overrideEntityMetadata);
        }
        else if (baseEntityMetadata != null)
        {
            Description = new EntityDescription(baseEntityMetadata);
        }
        else if (overrideEntityMetadata != null)
        {
            Description = new EntityDescription(overrideEntityMetadata);
        }
        else
            throw new InvalidOperationException("У сущности нет описания.");
    }
}
