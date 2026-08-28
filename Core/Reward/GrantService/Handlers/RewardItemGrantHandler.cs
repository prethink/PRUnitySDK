/// <summary>
/// Регистрирует обычные предметы в общей коллекции открытых предметов.
/// Проектные обработчики с более высоким приоритетом могут заменить это поведение.
/// </summary>
public sealed class RewardItemGrantHandler : IRewardGrantHandler
{
    /// <inheritdoc />
    public int Priority => -1000;

    /// <inheritdoc />
    public bool CanHandle(RewardGrantContext context)
    {
        return context?.Reward is RewardItemBase && context.Reward is not RewardResource;
    }

    /// <inheritdoc />
    public bool TryGrant(RewardGrantContext context)
    {
        var reward = context.Reward as RewardItemBase;
        if (reward?.Item == null || context.Multiplier > int.MaxValue)
            return false;

        OpenedItemsManager openedItems = PRUnitySDK.Managers.OpenedItems;
        if (openedItems == null)
        {
            PRLog.WriteWarning(
                typeof(RewardItemGrantHandler),
                "Cannot grant item reward before OpenedItemsManager is initialized.");
            return false;
        }

        string source = typeof(RewardGrantService).FullName;
        return openedItems.Add(
            source,
            reward.Item,
            (int)context.Multiplier,
            context.Save);
    }
}
