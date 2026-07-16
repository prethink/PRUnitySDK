using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeEntityBase : EntityBase, IEntityInfo
{
    [field: SerializeField] public Sprite Icon { get; protected set; }

    [field: SerializeField] public string LocalizationKey { get; protected set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    protected IEntityInfo baseEntityInfo;
    protected IEntityInfo overrideEntityInfo;

    protected override void InitializeEntityInfo()
    {
        Info = EntityUtils.GetEntityInfo(ref baseEntityInfo, ref overrideEntityInfo, this);
    }
}
