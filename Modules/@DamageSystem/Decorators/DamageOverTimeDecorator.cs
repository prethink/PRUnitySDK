using UnityEngine;

/// <summary>
/// Заготовка периодического урона.
/// </summary>
/// <remarks>
/// TODO: класс не дописан, <see cref="IDamageProvider"/> не реализует и в системе урона
/// не участвует. Тики не укладываются в этот контракт: он описывает разовый расчёт,
/// а тики идут во времени и прекращаются при смерти цели. Делать это лучше не
/// декоратором, а отдельной сущностью со своим жизненным циклом, например на базе
/// <see cref="TickDamageBrain"/>.
/// </remarks>
public class DamageOverTimeDecorator// : IDamageProvider
{
    /// <summary>
    /// Источник урона одного тика.
    /// </summary>
    private readonly IDamageProvider damageProvider;

    /// <summary>
    /// Интервал между тиками в секундах.
    /// </summary>
    private readonly float tickInterval;

    /// <summary>
    /// Количество тиков.
    /// </summary>
    private readonly int tickCount;

    /// <summary>
    /// Компонент, на котором должна выполняться корутина тиков.
    /// </summary>
    private readonly MonoBehaviour context;

    /// <summary>
    /// Создаёт описание периодического урона.
    /// </summary>
    /// <param name="damageProvider">Источник урона одного тика.</param>
    /// <param name="tickInterval">Интервал между тиками в секундах.</param>
    /// <param name="tickCount">Количество тиков.</param>
    /// <param name="context">Компонент-владелец корутины.</param>
    public DamageOverTimeDecorator(
        IDamageProvider damageProvider,
        float tickInterval,
        int tickCount,
        MonoBehaviour context)
    {
        this.damageProvider = damageProvider;
        this.tickInterval = tickInterval;
        this.tickCount = tickCount;
        this.context = context;
    }
}
