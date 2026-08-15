using System;
using System.Collections.Generic;

/// <summary>
/// Выдаёт награды через упорядоченную коллекцию специализированных обработчиков.
/// </summary>
public sealed class RewardGrantService : IRewardGrantService
{
    private readonly List<IRewardGrantHandler> handlers = new();

    /// <inheritdoc />
    public IReadOnlyList<IRewardGrantHandler> Handlers => handlers;

    /// <summary>
    /// Создаёт сервис со стандартными обработчиками публичного SDK.
    /// </summary>
    public RewardGrantService()
    {
        RegisterHandler(new RewardResourceGrantHandler());
        RegisterHandler(new RewardActionGrantHandler());
        RegisterHandler(new RewardItemGrantHandler());
    }

    /// <inheritdoc />
    public bool TryGrant(RewardDataBase reward, long executor = 0, long multiplier = 1, bool save = true)
    {
        return TryGrant(new RewardGrantContext(reward, executor, multiplier, save));
    }

    /// <inheritdoc />
    public bool TryGrant(RewardGrantContext context)
    {
        if (context?.Reward == null)
        {
            PRLog.WriteWarning(typeof(RewardGrantService), "Cannot grant a null reward.");
            return false;
        }

        if (!context.Reward.IsConfigured)
        {
            PRLog.WriteWarning(
                typeof(RewardGrantService),
                $"Cannot grant reward '{context.Reward.name}': it is not configured.");
            return false;
        }

        if (context.Multiplier <= 0)
        {
            PRLog.WriteWarning(
                typeof(RewardGrantService),
                $"Cannot grant reward '{context.Reward.name}' with multiplier {context.Multiplier}.");
            return false;
        }

        foreach (IRewardGrantHandler handler in handlers)
        {
            try
            {
                if (!handler.CanHandle(context))
                    continue;

                if (!handler.TryGrant(context))
                    return false;

                RewardEvents.RaiseGranted(context);
                return true;
            }
            catch (Exception exception)
            {
                PRLog.WriteError(
                    typeof(RewardGrantService),
                    $"Handler '{handler.GetType().Name}' failed to grant reward " +
                    $"'{context.Reward.name}'. {exception}");
                return false;
            }
        }

        PRLog.WriteWarning(
            typeof(RewardGrantService),
            $"No reward handler is registered for '{context.Reward.GetType().Name}'.");
        return false;
    }

    /// <inheritdoc />
    public bool RegisterHandler(IRewardGrantHandler handler)
    {
        if (handler == null)
            return false;

        foreach (IRewardGrantHandler registeredHandler in handlers)
        {
            if (registeredHandler.GetType() == handler.GetType())
                return false;
        }

        handlers.Add(handler);
        handlers.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        return true;
    }

    /// <inheritdoc />
    public bool UnregisterHandler(IRewardGrantHandler handler)
    {
        return handler != null && handlers.Remove(handler);
    }
}
