using UnityEngine;

public class DamageOverTimeDecorator// : IDamageProvider
{
    private readonly IDamageProvider damageProvider;
    private readonly float tickInterval;   // �������� ����� ������ �����
    private readonly int tickCount;        // ���������� �����
    private readonly MonoBehaviour context; // ����� ��� ������� ��������

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
    /// ��������� ������ DoT �� ������.
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

    //    //    // ������ ��� ������� ����
    //    //    victim.TakeDamage(attacker, weapon, damageProvider as DamageBase);

    //    //    // ����� ���������� �������: OnDotTick, OnDotFinished
    //    //}
    //}
}
