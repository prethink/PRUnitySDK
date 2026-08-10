/// <summary>
/// Точки публикации глобальных событий боевой системы.
/// </summary>
public static class CombatEvents 
{
    /// <summary>
    /// Публикует событие убийства уроном.
    /// </summary>
    /// <param name="args">Контекст убийства.</param>
    public static void RaiseOnKill(EntityKillEventArgs args) => EventBus.RaiseEvent<IEntityKillEvent>(x => x.OnKill(args));

    /// <summary>
    /// Публикует событие успешно применённого урона.
    /// </summary>
    /// <param name="args">Контекст урона.</param>
    public static void RaiseOnTakeDamage(TakeDamageEvent args) => EventBus.RaiseEvent<IOnTakeDamageEvents>(x => x.OnTakeDamage(args));

    /// <summary>
    /// Публикует итог любой попытки нанесения урона.
    /// </summary>
    /// <param name="args">Контекст и итог попытки.</param>
    public static void RaiseOnDamageProcessed(DamageProcessedEvent args) =>
        EventBus.RaiseEvent<IDamageProcessedEvents>(x => x.OnDamageProcessed(args));
}
