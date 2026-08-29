using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CommonEntity : EntityBase, IEntityMetadata
{
    public override Enumeration EntityType => Enumeration.GetOrCreate(EntityTypeValue);

    public override string Name => EntityName;

    [field: SerializeField, Header("EntityMetadata")] public string EntityName { get; protected set; }
    [field: SerializeField] public string EntityTypeValue { get; protected set; }

    [field: SerializeField] public Sprite Icon { get; protected set; }

    [field: SerializeField] public string LocalizationKey { get; protected set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    protected IEntityMetadata baseEntityMetadata;
    protected IEntityMetadata overrideEntityMetadata;

    protected override void InitializeEntityMetadata()
    {
        Info = EntityUtils.GetEntityMetadata(ref baseEntityMetadata, ref overrideEntityMetadata, this);
    }
}
