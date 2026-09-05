/// <summary>
/// Базовые данные глобального боевого события.
/// </summary>
public abstract class CombatEventBase : GameplayEventArgsBase
{
    /// <summary>
    /// Сущность, инициировавшая атаку.
    /// </summary>
    public IEntity Attacker { get; protected set; }

    /// <summary>
    /// Сущность, получившая или обрабатывающая атаку.
    /// </summary>
    public IEntity Victim { get; protected set; }

    /// <summary>
    /// Оружие атаки либо <c>null</c>, если урон нанесён без оружия.
    /// </summary>
    public IWeapon Weapon { get; protected set; }

    /// <summary>
    /// Создаёт боевое событие с оружием.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    /// <param name="weapon">Использованное оружие либо <c>null</c>.</param>
    public CombatEventBase(IEntity attacker, IEntity victim, IWeapon weapon) 
    {
        Attacker = attacker;
        Victim = victim;
        Weapon = weapon;
    }

    /// <summary>
    /// Создаёт боевое событие без оружия.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="victim">Сущность-жертва.</param>
    public CombatEventBase(IEntity attacker, IEntity victim)
        : this(attacker, victim, null)
    {

    }

    /// <summary>
    /// Возвращает категорию глобальных боевых событий.
    /// </summary>
    /// <returns>Путь категории Combat.</returns>
    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Combat");
    }
}

/// <summary>
/// Подписчик на глобальные события убийства уроном.
/// </summary>
public interface IEntityKillEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает убийство сущности.
    /// </summary>
    /// <param name="args">Контекст убийства.</param>
    void OnKill(EntityKillEventArgs args);
}

/// <summary>
/// Подписчик на глобальные события успешно применённого урона.
/// </summary>
public interface IOnTakeDamageEvents : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает обычный или смертельный применённый урон.
    /// </summary>
    /// <param name="args">Контекст применённого урона.</param>
    void OnTakeDamage(TakeDamageEvent args);
}

/// <summary>
/// Получает результат любой попытки нанесения урона, включая Miss и Blocked.
/// </summary>
public interface IDamageProcessedEvents : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает завершённую попытку нанесения урона.
    /// </summary>
    /// <param name="args">Контекст и итог попытки.</param>
    void OnDamageProcessed(DamageProcessedEvent args);
}
