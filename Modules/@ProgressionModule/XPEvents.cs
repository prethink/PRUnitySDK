/// <summary>
/// Точки публикации событий опыта и уровней игроков.
/// </summary>
public static class XPEvents
{
    /// <summary>
    /// Публикует изменение количества опыта игрока.
    /// </summary>
    public static void RaiseExperienceChanged(IPlayer player, XPData previous, XPData current) =>
        EventBus.RaiseEvent<IXPChangedEvent>(subscriber =>
            subscriber.OnExperienceChanged(new XPChangedEventArgs(player, previous, current)));

    /// <summary>
    /// Публикует изменение уровня игрока в любую сторону.
    /// </summary>
    public static void RaiseLevelChanged(IPlayer player, XPData previous, XPData current) =>
        EventBus.RaiseEvent<IXPLevelChangedEvent>(subscriber =>
            subscriber.OnLevelChanged(new XPChangedEventArgs(player, previous, current)));

    /// <summary>
    /// Публикует повышение уровня игрока.
    /// </summary>
    public static void RaiseLevelUp(IPlayer player, XPData previous, XPData current) =>
        EventBus.RaiseEvent<IXPLevelUpEvent>(subscriber =>
            subscriber.OnLevelUp(new XPChangedEventArgs(player, previous, current)));
}

/// <summary>
/// Неизменяемый контекст изменения прогресса конкретного игрока.
/// </summary>
public sealed class XPChangedEventArgs : GameplayEventArgsBase
{
    /// <summary>
    /// Игрок, прогресс которого изменился.
    /// </summary>
    public IPlayer Player { get; }

    /// <summary>
    /// Прогресс до изменения.
    /// </summary>
    public XPData Previous { get; }

    /// <summary>
    /// Прогресс после изменения.
    /// </summary>
    public XPData Current { get; }

    /// <summary>
    /// Фактическое изменение общего количества опыта.
    /// </summary>
    public long ExperienceDelta => Current.CurrentScore - Previous.CurrentScore;

    /// <summary>
    /// Фактическое изменение уровня.
    /// </summary>
    public long LevelDelta => Current.CurrentLevel - Previous.CurrentLevel;

    /// <summary>
    /// Создаёт контекст изменения прогресса игрока.
    /// </summary>
    /// <param name="player">Игрок, прогресс которого изменился.</param>
    /// <param name="previous">Прогресс до изменения.</param>
    /// <param name="current">Прогресс после изменения.</param>
    public XPChangedEventArgs(IPlayer player, XPData previous, XPData current)
    {
        Player = player;
        Previous = previous;
        Current = current;
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "XP");
    }
}

/// <summary>
/// Подписчик на изменение количества опыта игрока.
/// </summary>
public interface IXPChangedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает изменение количества опыта игрока.
    /// </summary>
    /// <param name="args">Контекст изменения прогресса.</param>
    void OnExperienceChanged(XPChangedEventArgs args);
}

/// <summary>
/// Подписчик на изменение уровня игрока в любую сторону.
/// </summary>
public interface IXPLevelChangedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает изменение уровня игрока в любую сторону.
    /// </summary>
    /// <param name="args">Контекст изменения прогресса.</param>
    void OnLevelChanged(XPChangedEventArgs args);
}

/// <summary>
/// Подписчик на повышение уровня игрока.
/// </summary>
public interface IXPLevelUpEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает повышение уровня игрока.
    /// </summary>
    /// <param name="args">Контекст изменения прогресса.</param>
    void OnLevelUp(XPChangedEventArgs args);
}
