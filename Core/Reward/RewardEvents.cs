/// <summary>
/// Публикует события системы наград.
/// </summary>
public static class RewardEvents
{
    /// <summary>
    /// Уведомляет подписчиков после успешной выдачи награды.
    /// </summary>
    public static void RaiseGranted(RewardGrantContext context)
    {
        EventBus.RaiseEvent<IRewardGrantedEvent>(subscriber => subscriber.OnRewardGranted(context));
    }
}
