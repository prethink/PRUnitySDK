using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward resource", menuName = "PRUnitySDK/Reward/Reward resource")]
public class RewardResource : RewardItemBase
{
    [field: SerializeField] public int Count { get; protected set; }
    [field: SerializeField] public ResourceItemDefinition ResourceData { get; protected set; }

    public override ItemDefinitionBase Item => ResourceData;

    public override Sprite Icon => ResourceData.Icon;

    public override string LocalizationKey => ResourceData.LocalizationKey;

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => ResourceData.LocalizationValues;
}
