using UnityEngine;

/// <summary>
/// Заготовка периодического урона: наносить его тиками через равные промежутки.
/// <para>
/// ВНИМАНИЕ: класс не дописан и в системе урона не участвует. Он не реализует
/// <see cref="IDamageProvider"/>, а вся логика тиков закомментирована - конструктор
/// лишь сохраняет параметры. Оставлен как набросок будущей реализации.
/// </para>
/// <para>
/// Периодический урон не укладывается в контракт <see cref="IDamageProvider"/>: тот
/// описывает разовый расчёт данных, а тики нужно проводить во времени и прекращать
/// при смерти цели или снятии эффекта. Поэтому доделывать его стоит не декоратором,
/// а отдельной сущностью со своим жизненным циклом - например, на базе
/// <see cref="TickDamageBrain"/>, который уже умеет наносить урон по расписанию.
/// </para>
/// </summary>
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
