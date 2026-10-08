using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Награда-контейнер с собственным набором наград.
/// </summary>
public abstract class RewardContainerBase : RewardBase
{
    [SerializeField] private string id = Guid.NewGuid().ToString();
    [SerializeField, SpritePreview(140)] private Sprite icon;
    [SerializeField, SerializedDictionary("Lang", "Value")]
    private SerializedDictionary<LangType, string> localization = new();
    [SerializeField, Min(1)] private int previewCount = 20;

    [SerializeField]
    [Tooltip("Гарант редкой награды: шанс редкого растёт с каждым открытием без него. Счётчик у контейнера свой.")]
    private RewardPitySettings pity = new();

    [SerializeField]
    [Tooltip("Показывать игроку шанс каждой награды в окне, которое разыгрывает контейнер.")]
    private bool showChances;

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
    public bool TryRoll(out RewardBase reward)
    {
        return WeightUtils.TryGetRandom(
            Rewards,
            out reward,
            configuredReward => configuredReward != null &&
                                configuredReward != this &&
                                configuredReward.IsConfigured);
    }

    /// <summary>
    /// Настройки гаранта редкой награды.
    /// </summary>
    public RewardPitySettings Pity => pity;

    /// <summary>
    /// Выбирает награду, которая достанется игроку: по весам с учётом гаранта.
    /// </summary>
    /// <remarks>
    /// Отдельно от <see cref="TryRoll"/>: тот разыгрывает и карточки-попутчики в ленте кейса,
    /// и выигрыши ботов, а им гарант не положен. Счётчик метод не двигает — это делает
    /// <see cref="RegisterRoll"/>, когда награда действительно выдана.
    /// </remarks>
    public bool TryRollWithPity(out RewardBase reward)
    {
        return RewardPity.TryRoll(
            Rewards,
            configuredReward => configuredReward != null &&
                                configuredReward != this &&
                                configuredReward.IsConfigured,
            pity,
            RewardPity.GetMisses(id),
            out reward);
    }

    /// <summary>
    /// Показывать ли игроку шанс каждой награды.
    /// </summary>
    public bool ShowChances => showChances;

    /// <summary>
    /// Шанс награды в следующем открытии, от нуля до единицы, с учётом гаранта.
    /// </summary>
    public double GetChance(RewardBase reward)
    {
        return RewardPity.GetChance(
            Rewards,
            configuredReward => configuredReward != null &&
                                configuredReward != this &&
                                configuredReward.IsConfigured,
            pity,
            RewardPity.GetMisses(id),
            reward);
    }

    /// <summary>
    /// Учитывает выданную награду в счётчике гаранта.
    /// </summary>
    public void RegisterRoll(RewardBase reward, bool save = true)
    {
        RewardPity.Register(pity, id, reward, save);
    }
}
