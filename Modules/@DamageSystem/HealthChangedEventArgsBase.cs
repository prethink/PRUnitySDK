/// <summary>
/// Данные об одном изменении здоровья сущности.
/// </summary>
public class HealthChangedEventArgsBase
{
    /// <summary>
    /// Здоровье до изменения.
    /// </summary>
    public float PreviousHealth { get; }

    /// <summary>
    /// Здоровье после изменения.
    /// </summary>
    public float CurrentHealth { get; }

    /// <summary>
    /// Максимальное здоровье сущности.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// Разница между текущим и предыдущим здоровьем; отрицательна при уроне.
    /// </summary>
    public float Delta => CurrentHealth - PreviousHealth;

    /// <summary>
    /// Результат урона, вызвавшего изменение; <c>null</c> для лечения и других причин.
    /// </summary>
    public DamageOutcome DamageOutcome { get; }

    /// <summary>
    /// Изменение принесло стартовые значения, а не урон или лечение.
    /// </summary>
    /// <remarks>
    /// Так приходит <see cref="HealthComponent.InitHealth"/>: сущность начинает жизнь
    /// заново — на сцене, из пула или возвращённой после смерти, — и здоровье встаёт
    /// на потолок. Разница при этом положительная и от настоящего лечения не отличается,
    /// поэтому её помечают: полоса над головой поднимается от лечения, и без метки она
    /// выскакивала бы над каждой только что появившейся сущностью.
    /// </remarks>
    public bool IsInitial { get; }

    /// <summary>
    /// Создаёт данные без известного предыдущего значения для обратной совместимости.
    /// </summary>
    /// <param name="currentHealth">Текущее здоровье.</param>
    /// <param name="maxHealth">Максимальное здоровье.</param>
    public HealthChangedEventArgsBase(float currentHealth, float maxHealth)
        : this(currentHealth, currentHealth, maxHealth, null)
    {
    }

    /// <summary>
    /// Создаёт полные данные об изменении здоровья.
    /// </summary>
    /// <param name="previousHealth">Здоровье до изменения.</param>
    /// <param name="currentHealth">Здоровье после изменения.</param>
    /// <param name="maxHealth">Максимальное здоровье.</param>
    /// <param name="damageOutcome">Связанный результат урона либо <c>null</c>.</param>
    /// <param name="isInitial">Изменение принесло стартовые значения.</param>
    public HealthChangedEventArgsBase(
        float previousHealth,
        float currentHealth,
        float maxHealth,
        DamageOutcome damageOutcome = null,
        bool isInitial = false)
    {
        PreviousHealth = previousHealth;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        DamageOutcome = damageOutcome;
        IsInitial = isInitial;
    }
}
