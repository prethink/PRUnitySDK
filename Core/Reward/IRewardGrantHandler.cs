/// <summary>
/// Обрабатывает поддерживаемый тип награды.
/// </summary>
public interface IRewardGrantHandler : IPrioritized
{
    /// <summary>
    /// Проверяет, может ли обработчик полностью выдать указанную награду.
    /// </summary>
    bool CanHandle(RewardGrantContext context);

    /// <summary>
    /// Выдаёт награду. Возвращает <see langword="true"/> только после успешной выдачи.
    /// </summary>
    bool TryGrant(RewardGrantContext context);
}
