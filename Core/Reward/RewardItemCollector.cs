using System.Collections.Generic;

/// <summary>
/// Достаёт из награды предметы, которые она может выдать.
/// </summary>
/// <remarks>
/// Награда бывает контейнером с другими наградами, а те — снова контейнерами. Разбор
/// одинаков для всех, кто хочет знать состав: достижений, подарков, кейсов, — поэтому
/// живёт здесь, а не копией в каждой системе.
/// </remarks>
public static class RewardItemCollector
{
    /// <summary>
    /// Идентификаторы предметов внутри награды.
    /// </summary>
    public static IEnumerable<string> GetItemIds(RewardBase reward)
    {
        var ids = new List<string>();
        var visited = new HashSet<RewardBase>();

        Collect(reward, ids, visited);
        return ids;
    }

    /// <summary>
    /// Идентификаторы предметов внутри набора наград.
    /// </summary>
    public static IEnumerable<string> GetItemIds(IEnumerable<RewardBase> rewards)
    {
        var ids = new List<string>();
        var visited = new HashSet<RewardBase>();

        if (rewards == null)
            return ids;

        foreach (RewardBase reward in rewards)
            Collect(reward, ids, visited);

        return ids;
    }

    /// <summary>
    /// Идентификаторы предметов внутри взвешенного набора.
    /// </summary>
    public static IEnumerable<string> GetItemIds(IEnumerable<WeightedRewardEntry> entries)
    {
        var ids = new List<string>();
        var visited = new HashSet<RewardBase>();

        if (entries == null)
            return ids;

        foreach (WeightedRewardEntry entry in entries)
            Collect(entry?.Item, ids, visited);

        return ids;
    }

    /// <summary>
    /// Разбирает награду, спускаясь по вложенным контейнерам.
    /// </summary>
    /// <remarks>
    /// Уже разобранные награды пропускаются: контейнер вполне может встретиться дважды,
    /// а по кольцу ссылок разбор ушёл бы в бесконечность.
    /// </remarks>
    private static void Collect(RewardBase reward, List<string> ids, HashSet<RewardBase> visited)
    {
        if (reward == null || !visited.Add(reward))
            return;

        if (reward is RewardItemBase itemReward && itemReward.Item != null)
        {
            ids.Add(itemReward.Item.Id);
            return;
        }

        if (reward is not RewardContainerBase container)
            return;

        IReadOnlyList<WeightedRewardEntry> entries = container.Rewards;

        if (entries == null)
            return;

        foreach (WeightedRewardEntry entry in entries)
            Collect(entry?.Item, ids, visited);
    }
}
