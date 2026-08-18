/// <summary>
/// Неизменяемый контекст одной операции выдачи награды.
/// </summary>
public sealed class RewardGrantContext
{
    /// <summary>
    /// Выдаваемая награда.
    /// </summary>
    public RewardBase Reward { get; }

    /// <summary>
    /// Идентификатор игрока или другого инициатора операции.
    /// </summary>
    public long Executor { get; }

    /// <summary>
    /// Игрок-получатель, если вызывающая сторона передала его напрямую.
    /// </summary>
    public IPlayer Player { get; }

    /// <summary>
    /// Множитель количества. Для уникальных предметов обработчик может его игнорировать.
    /// </summary>
    public long Multiplier { get; }

    /// <summary>
    /// Нужно ли сохранить изменённые данные после выдачи.
    /// </summary>
    public bool Save { get; }

    /// <summary>
    /// Создаёт контекст по идентификатору исполнителя.
    /// </summary>
    public RewardGrantContext(
        RewardBase reward,
        long executor = 0,
        long multiplier = 1,
        bool save = true,
        IPlayer player = null)
    {
        Reward = reward;
        Player = player;
        Executor = player?.PlayerId ?? executor;
        Multiplier = multiplier;
        Save = save;
    }

    /// <summary>
    /// Возвращает переданного игрока либо ищет его в текущем <see cref="PlayerTracker"/>.
    /// </summary>
    public bool TryGetPlayer(out IPlayer player)
    {
        player = Player;
        if (player != null && !player.IsNull())
            return true;

        foreach (IPlayer trackedPlayer in PRUnitySDK.Trackers.Players.Players)
        {
            if (trackedPlayer.PlayerId != Executor)
                continue;

            player = trackedPlayer;
            return true;
        }

        player = null;
        return false;
    }
}
