using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Периодический урон: горение, отравление, кровотечение.
/// </summary>
/// <remarks>
/// Не декоратор и не модификатор. <see cref="IDamageProvider"/> описывает разовый расчёт,
/// а тик идёт во времени, прекращается со смертью цели и переживает того, кто его
/// поставил. Поэтому у эффекта свой жизненный цикл — корутина.
/// <para>
/// Отсчёт идёт игровым временем и встаёт на логической паузе: замедление растягивает
/// горение вместе с игрой, а открытое меню его не проматывает.
/// </para>
/// <para>
/// Владельца (<c>instance</c>) можно не задавать — тогда корутина живёт на глобальном
/// хосте и не обрывается вместе с тем, кто её запустил. Ядовитая лужа исчезает, а
/// отравление продолжается; проверку «цель ещё жива и на сцене» эффект делает сам.
/// </para>
/// </remarks>
public class DamageOverTimeCoroutine : PRCoroutineBase
{
    /// <summary>
    /// Тикать, пока эффект не остановят или цель не выйдет из-под урона.
    /// </summary>
    public const int Infinite = -1;

    private readonly HealthComponent target;
    private readonly IEntity attacker;
    private readonly IWeapon weapon;
    private readonly IDamageProvider damage;
    private readonly float tickInterval;
    private readonly int tickCount;
    private readonly Action completed;

    /// <summary>
    /// Сколько тиков успело пройти.
    /// </summary>
    public int TicksDone { get; private set; }

    /// <summary>
    /// Эффект отработал: тики кончились либо цель вышла из-под урона.
    /// </summary>
    /// <remarks>
    /// Остановка через <see cref="PRCoroutineBase.Stop"/> сюда не попадает: Unity
    /// обрывает корутину, и последняя строка до конца не доходит.
    /// </remarks>
    public bool IsFinished { get; private set; }

    /// <summary>
    /// Создаёт эффект периодического урона.
    /// </summary>
    /// <param name="target">Здоровье цели.</param>
    /// <param name="attacker">Кто поставил эффект; может быть <c>null</c>.</param>
    /// <param name="weapon">Чем поставлен эффект; может быть <c>null</c>.</param>
    /// <param name="damage">Урон одного тика.</param>
    /// <param name="tickInterval">Секунды между тиками.</param>
    /// <param name="tickCount">Сколько тиков; <see cref="Infinite"/> — без конца.</param>
    /// <param name="completed">Вызывается, когда эффект отработал сам.</param>
    /// <param name="instance">Владелец корутины; <c>null</c> — глобальный хост.</param>
    public DamageOverTimeCoroutine(
        HealthComponent target,
        IEntity attacker,
        IWeapon weapon,
        IDamageProvider damage,
        float tickInterval,
        int tickCount = Infinite,
        Action completed = null,
        MonoBehaviour instance = null)
        : base(instance)
    {
        this.target = target != null ? target : throw new ArgumentNullException(nameof(target));
        this.damage = damage ?? throw new ArgumentNullException(nameof(damage));
        this.attacker = attacker;
        this.weapon = weapon;
        this.tickInterval = Mathf.Max(0.01f, tickInterval);
        this.tickCount = tickCount;
        this.completed = completed;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Первый тик приходит не сразу, а через интервал: эффект ставят вместе с попаданием,
    /// и мгновенный тик выглядел бы как двойной урон от одного удара.
    /// </remarks>
    protected override IEnumerator InternalExecute()
    {
        IsFinished = false;
        TicksDone = 0;

        int ticksLeft = tickCount;

        while (ticksLeft != 0)
        {
            yield return WaitInterval();

            // Спрашиваем перед каждым тиком, а не один раз при запуске: за интервал цель
            // успевает умереть, спрятаться или уйти в пул.
            if (!CanDamage())
                break;

            target.TakeDamage(attacker, weapon, BuildTickDamage());
            TicksDone++;

            if (ticksLeft > 0)
                ticksLeft--;
        }

        IsFinished = true;
        completed?.Invoke();
    }

    /// <summary>
    /// Ждёт интервал между тиками.
    /// </summary>
    /// <remarks>
    /// На логической паузе ожидание стоит, а не сбрасывается: после её снятия отсчёт
    /// продолжается с того же места, и меню не превращается в лишний тик.
    /// </remarks>
    private IEnumerator WaitInterval()
    {
        float left = tickInterval;

        while (left > 0f)
        {
            yield return WaitPause.Instance;

            left -= PRTime.Instance.GameDeltaTime;

            yield return null;
        }
    }

    /// <summary>
    /// Можно ли ещё наносить урон этой цели.
    /// </summary>
    /// <remarks>
    /// <c>TakeDamage</c> отказал бы и сам, но эффект должен не просто промахиваться,
    /// а закончиться: иначе горение мёртвой сущности тикало бы вхолостую до конца
    /// сцены, а у бесконечного эффекта — вечно.
    /// </remarks>
    private bool CanDamage()
    {
        // Unity-null: сущность могли уничтожить, а корутина на глобальном хосте
        // об этом не узнает.
        if (target == null)
            return false;

        if (!target.IsAlive() || target.IsBlockDamage || !target.CanTakeDamage())
            return false;

        EntityBase entity = target.Entity;

        // Спрятанная или убранная в пул сущность на сцене не присутствует. Жечь её
        // незачем: вернётся она уже новой жизнью, с полным здоровьем.
        return entity == null || (entity.OnScene && !entity.InPool);
    }

    /// <summary>
    /// Собирает урон одного тика.
    /// </summary>
    /// <remarks>
    /// Каждый тик — отдельное попадание, поэтому ему выдаётся свой
    /// <see cref="DamageData.DamageId"/>: общий склеил бы тики в одну серию, как дробь
    /// из одного выстрела.
    /// <para>
    /// Пометка <see cref="DamageType.TimeBased"/> ставится здесь, а не вызывающим: тик
    /// эффекта отличается от одиночного удара самим фактом, и забыть её нельзя.
    /// </para>
    /// </remarks>
    private IDamageProvider BuildTickDamage()
    {
        DamageData source = damage.GetDamageData();

        if (source == null)
            return damage;

        DamageData data = source.Clone();

        data.DamageId = Guid.NewGuid();
        data.DamageType |= DamageType.TimeBased;

        return Damages.From(data);
    }
}
