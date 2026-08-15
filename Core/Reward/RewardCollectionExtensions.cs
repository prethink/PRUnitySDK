using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Фильтры для коллекций наград.
/// </summary>
public static class RewardCollectionExtensions
{
    /// <summary>
    /// Возвращает только настроенные ресурсные награды.
    /// </summary>
    public static IEnumerable<RewardResource> GetOnlyResources(this IEnumerable<RewardDataBase> rewards)
    {
        return rewards?.OfType<RewardResource>().Where(reward => reward.IsConfigured)
               ?? Enumerable.Empty<RewardResource>();
    }

    /// <summary>
    /// Возвращает настроенные предметные награды, исключая ресурсы.
    /// </summary>
    public static IEnumerable<RewardItemBase> GetOnlyItems(this IEnumerable<RewardDataBase> rewards)
    {
        return rewards?.OfType<RewardItemBase>()
                   .Where(reward => reward is not RewardResource && reward.IsConfigured)
               ?? Enumerable.Empty<RewardItemBase>();
    }

    /// <summary>
    /// Возвращает ненулевые и полностью настроенные награды.
    /// </summary>
    public static IEnumerable<RewardDataBase> GetConfiguredRewards(this IEnumerable<RewardDataBase> rewards)
    {
        return rewards?.Where(reward => reward != null && reward.IsConfigured)
               ?? Enumerable.Empty<RewardDataBase>();
    }

    /// <summary>
    /// Исключает уже открытые уникальные предметы с помощью переданного правила владения.
    /// Ресурсы и action-награды остаются доступными.
    /// </summary>
    public static IEnumerable<RewardDataBase> GetAvailableRewards(
        this IEnumerable<RewardDataBase> rewards,
        Func<RewardItemBase, bool> isOpened)
    {
        IEnumerable<RewardDataBase> configuredRewards = rewards.GetConfiguredRewards();
        if (isOpened == null)
            return configuredRewards;

        return configuredRewards.Where(reward =>
            reward is not RewardItemBase itemReward ||
            reward is RewardResource ||
            !isOpened(itemReward));
    }
}
