/// <summary>
/// Выполняет action-награды.
/// </summary>
public sealed class RewardActionGrantHandler : IRewardGrantHandler
{
    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public bool CanHandle(RewardGrantContext context)
    {
        return context?.Reward is RewardAction;
    }

    /// <inheritdoc />
    public bool TryGrant(RewardGrantContext context)
    {
        var reward = context.Reward as RewardAction;
        if (reward == null || !reward.IsConfigured)
            return false;

        reward.InvokeAction();
        return true;
    }
}
