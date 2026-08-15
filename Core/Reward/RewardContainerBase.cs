using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Награда-контейнер с собственным набором наград.
/// </summary>
public abstract class RewardContainerBase : RewardDataBase
{
    [SerializeField] private string id = Guid.NewGuid().ToString();
    [SerializeField, SpritePreview(140)] private Sprite icon;
    [SerializeField, SerializedDictionary("Lang", "Value")]
    private SerializedDictionary<LangType, string> localization = new();
    [SerializeField, Min(1)] private int previewCount = 20;

    /// <summary>
    /// Стабильный идентификатор контейнера.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Награды и их фактические веса.
    /// </summary>
    public abstract IReadOnlyList<WeightedRewardEntry> Rewards { get; }

    /// <summary>
    /// Количество промежуточных элементов при визуальном открытии.
    /// </summary>
    public int PreviewCount => Mathf.Max(1, previewCount);

    public override Sprite Icon => icon;
    public override string LocalizationKey => $"RewardContainer_{id}";
    public override IReadOnlyDictionary<LangType, string> LocalizationValues => localization;

    /// <summary>
    /// Пытается выбрать одну настроенную награду.
    /// </summary>
    public bool TryRoll(out RewardDataBase reward)
    {
        return WeightUtils.TryGetRandom(
            Rewards,
            out reward,
            configuredReward => configuredReward != null &&
                                configuredReward != this &&
                                configuredReward.IsConfigured);
    }
}
