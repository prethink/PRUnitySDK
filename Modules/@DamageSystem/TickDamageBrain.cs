using UnityEngine;

public class TickDamageBrain : DamageBrainBase
{
    [field: SerializeField] public float TickInterval { get; protected set; }
    [field: SerializeField] public float DamagePerTick { get; protected set; }

    private float nextTimeTime = 0;

    public override void PerformAttack()
    {
        nextTimeTime = PRTime.Instance.Time + TickInterval;
    }

    public override bool CanAttack()
    {
        return nextTimeTime > PRTime.Instance.Time;
    }

    public override bool CanAttackSource()
    {
        return false;
    }

    protected override void PRUpdate()
    {
        base.PRUpdate();

        if(CanAttack())
        {
            PerformAttack();
        }
    }
}
