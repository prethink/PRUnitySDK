using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CommonEntity : EntityBase, IEntityInfo
{
    public override Enumeration EntityType => Enumeration.GetOrCreate(EntityTypeValue);

    public override string Name => EntityName;

    [field: SerializeField, Header("EntityInfo")] public string EntityName { get; protected set; }
    [field: SerializeField] public string EntityTypeValue { get; protected set; }

    [field: SerializeField] public Sprite Icon { get; protected set; }

    [field: SerializeField] public string LocalizationKey { get; protected set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    protected IEntityInfo baseEntityInfo;
    protected IEntityInfo overrideEntityInfo;

    protected override void InitializeEntityInfo()
    {
        baseEntityInfo = this;
        overrideEntityInfo = GetComponent<IEntityInfoProvider>()?.EntityInfo;

        if (baseEntityInfo != null && overrideEntityInfo != null)
        {
            Info = new EntityInfoContainer(baseEntityInfo, overrideEntityInfo);
        }
        else if (baseEntityInfo != null)
        {
            Info = new EntityInfoContainer(baseEntityInfo);
        }
        else if (overrideEntityInfo != null)
        {
            Info = new EntityInfoContainer(overrideEntityInfo);
        }
        else
            throw new InvalidOperationException("Not have entity info");
    }
}
