/// <summary>
/// Получает уведомление после успешной выдачи награды.
/// </summary>
public interface IRewardGrantedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после того, как один из обработчиков успешно выдал награду.
    /// </summary>
    void OnRewardGranted(RewardGrantContext context);
}
