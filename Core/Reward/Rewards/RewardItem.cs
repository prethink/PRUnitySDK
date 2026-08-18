using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Представляет произвольный <see cref="ItemDefinitionBase"/> в качестве награды.
/// </summary>
[CreateAssetMenu(fileName = "Item Reward", menuName = "PRUnitySDK/Reward/Item")]
public sealed class RewardItem : RewardItemBase
{
    [field: SerializeField]
    public ItemDefinitionBase ItemDefinition { get; private set; }

    public override ItemDefinitionBase Item => ItemDefinition;
    public override Sprite Icon => ItemDefinition != null ? ItemDefinition.Icon : null;
    public override string LocalizationKey => ItemDefinition != null ? ItemDefinition.LocalizationKey : string.Empty;
    public override IReadOnlyDictionary<LangType, string> LocalizationValues =>
        ItemDefinition != null ? ItemDefinition.LocalizationValues : EmptyLocalization.Values;

    /// <summary>
    /// Назначить предмет, представляемый этой наградой.
    /// </summary>
    public void Initialize(ItemDefinitionBase itemDefinition)
    {
        ItemDefinition = itemDefinition;
    }

    private static class EmptyLocalization
    {
        public static readonly IReadOnlyDictionary<LangType, string> Values =
            new Dictionary<LangType, string>();
    }
}
