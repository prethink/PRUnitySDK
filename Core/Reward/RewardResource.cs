using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward resource", menuName = "PRUnitySDK/Reward/Reward resource")]
public class RewardResource : RewardItemBase
{
    [field: SerializeField] public int Count { get; protected set; }

    [field: SerializeField] public int Multiply { get; protected set; }
    [field: SerializeField] public ResourceItemDefinition ResourceData { get; protected set; }

    public override ItemDefinitionBase Item => ResourceData;

    public override Sprite Icon => ResourceData.Icon;

    public override string LocalizationKey => ResourceData.LocalizationKey;

    public override IReadOnlyDictionary<LangType, string> LocalizationValues => ResourceData.LocalizationValues;

    public override bool IsConfigured => base.IsConfigured && Count > 0;

    public bool CanMultiply => Multiply > 1;

    /// <summary>
    /// Настраивает ресурсную награду.
    /// </summary>
    /// <param name="resourceData">Выдаваемый ресурс.</param>
    /// <param name="count">Базовое количество ресурса.</param>
    /// <param name="multiply">Предлагаемый коэффициент дополнительного умножения.</param>
    public void Initialize(ResourceItemDefinition resourceData, int count, int multiply = 1)
    {
        ResourceData = resourceData;
        Count = Mathf.Max(1, count);
        Multiply = Mathf.Max(1, multiply);
    }
}
