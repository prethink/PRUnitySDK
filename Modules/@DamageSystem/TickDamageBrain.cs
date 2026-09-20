using UnityEngine;

/// <summary>
/// Основа периодической атаки: отмеряет ритм тиков.
/// </summary>
/// <remarks>
/// Абстрактный намеренно. Здесь только ритм — кого бить, класс не знает: у зоны урона
/// это все, кто внутри, у ловушки — тот, кто наступил, у ауры — ближайший. Цель выбирает
/// наследник и наносит урон в <see cref="Attack"/>.
/// <para>
/// Раньше класс был обычным. Его можно было повесить на префаб, заполнить интервал
/// с уроном и ждать урона, которого не будет: <see cref="PerformAttack"/> только двигал
/// таймер, а <c>Attacker</c> с <c>DamageProvider</c> никто не заполнял. Ошибки при этом
/// не возникало — просто ничего не происходило.
/// </para>
/// <para>
/// Для урона по одной известной цели объект на сцене не нужен вовсе: горение и отравление
/// делает <see cref="DamageOverTimeCoroutine"/>.
/// </para>
/// </remarks>
public abstract class TickDamageBrain : DamageBrainBase
{
    [field: SerializeField, Min(0.01f)]
    [field: Tooltip("Секунды между тиками.")]
    public float TickInterval { get; protected set; } = 1f;

    [field: SerializeField, Min(0f)]
    [field: Tooltip("Урон одного тика.")]
    public float DamagePerTick { get; protected set; }

    /// <summary>
    /// Игровое время, начиная с которого разрешён следующий тик.
    /// </summary>
    private float nextTickTime;

    /// <summary>
    /// Наносит урон тика.
    /// </summary>
    /// <remarks>
    /// Вызывается, когда интервал вышел. Цель и способ попадания — забота наследника:
    /// он же заполняет <see cref="DamageBrainBase.Attacker"/>
    /// и <see cref="DamageBrainBase.DamageProvider"/>.
    /// </remarks>
    protected abstract void Attack();

    protected override void Start()
    {
        base.Start();

        // Первый тик через интервал, а не в первом же кадре: иначе зона бьёт в момент
        // появления, ещё до того, как в неё кто-то успел войти.
        nextTickTime = PRTime.Instance.GameTime + TickInterval;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Двигает таймер и бьёт. Раньше делал только первое, и в этом был весь изъян.
    /// </remarks>
    public override void PerformAttack()
    {
        nextTickTime = PRTime.Instance.GameTime + TickInterval;

        Attack();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Время игровое, поэтому на логической паузе тики не копятся, а замедление
    /// растягивает их вместе с игрой.
    /// </remarks>
    public override bool CanAttack()
    {
        return PRTime.Instance.GameTime >= nextTickTime;
    }

    /// <inheritdoc />
    public override bool CanAttackSource()
    {
        return false;
    }

    protected override void PRUpdate()
    {
        base.PRUpdate();

        if (CanAttack())
            PerformAttack();
    }
}
