using System.Collections.Generic;

/// <summary>
/// Выдаёт награды и управляет обработчиками конкретных типов наград.
/// </summary>
public interface IRewardGrantService
{
    /// <summary>
    /// Зарегистрированные обработчики в порядке выполнения.
    /// </summary>
    IReadOnlyList<IRewardGrantHandler> Handlers { get; }

    /// <summary>
    /// Выдаёт награду указанному исполнителю.
    /// </summary>
    bool TryGrant(RewardDataBase reward, long executor = 0, long multiplier = 1, bool save = true);

    /// <summary>
    /// Выдаёт награду с указанным контекстом.
    /// </summary>
    bool TryGrant(RewardGrantContext context);

    /// <summary>
    /// Регистрирует обработчик. Обработчик с таким же конкретным типом повторно не добавляется.
    /// </summary>
    bool RegisterHandler(IRewardGrantHandler handler);

    /// <summary>
    /// Удаляет ранее зарегистрированный экземпляр обработчика.
    /// </summary>
    bool UnregisterHandler(IRewardGrantHandler handler);
}
