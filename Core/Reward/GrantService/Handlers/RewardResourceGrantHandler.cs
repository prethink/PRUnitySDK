using System;

/// <summary>
/// Выдаёт ресурсы через общий кошелёк SDK.
/// </summary>
public sealed class RewardResourceGrantHandler : IRewardGrantHandler
{
    private readonly WalletResources wallet = new();

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public bool CanHandle(RewardGrantContext context)
    {
        return context?.Reward is RewardResource;
    }

    /// <inheritdoc />
    public bool TryGrant(RewardGrantContext context)
    {
        var reward = context.Reward as RewardResource;
        if (reward == null || !reward.IsConfigured || !wallet.IsConfigured(reward.ResourceData))
            return false;

        long amount;
        try
        {
            amount = checked((long)reward.Count * context.Multiplier);
        }
        catch (OverflowException)
        {
            PRLog.WriteWarning(
                typeof(RewardResourceGrantHandler),
                $"Resource reward '{reward.name}' amount overflowed Int64.");
            return false;
        }

        if (amount <= 0)
            return false;

        wallet.Add(reward.ResourceData, amount, context.Save);
        return true;
    }
}
