using UnityEngine;

public class DamageOverTimeDecorator// : IDamageProvider
{
    private readonly IDamageProvider damageProvider;
    private readonly float tickInterval;   // интервал между тиками урона
    private readonly int tickCount;        // количество тиков
    private readonly MonoBehaviour context; // нужно для запуска корутины

    //public DamageType DamageType => damageProvider.DamageType | DamageType.TimeBased;

    //public float GetDamageData() => damageProvider.GetDamageData();

    //public float GetKnockbackForce()
    //{
    //    return damageProvider.GetKnockbackForce();
    //}


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

    /// <summary>
    /// Запускает эффект DoT на жертве.
    /// </summary>
    //public void Apply(IEntity attacker, IEntity victim, IWeapon weapon)
    //{
    //    context.StartCoroutine(ApplyDamageOverTime(attacker, victim, weapon));
    //}

    //private IEnumerator ApplyDamageOverTime(IEntity attacker, IEntity victim, IWeapon weapon)
    //{

    //    //for (int i = 0; i < tickCount; i++)
    //    //{
    //    //    yield return new WaitForSeconds(tickInterval);

    //    //    // каждый тик наносим урон
    //    //    victim.TakeDamage(attacker, weapon, damageProvider as DamageBase);

    //    //    // Можно триггерить события: OnDotTick, OnDotFinished
    //    //}
    //}
}
