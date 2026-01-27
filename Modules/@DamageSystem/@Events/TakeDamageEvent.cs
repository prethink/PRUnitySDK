public class TakeDamageEvent : CombatEventBase
{
    public DamageData Damage { get; protected set; }

    public TakeDamageEvent(IEntity attacker, IEntity victim, DamageData damage, IWeapon weapon)
        : base(attacker, victim, weapon)
    {
        Damage = damage;
    }

    public TakeDamageEvent(IEntity attacker, IEntity victim, DamageData damage)
        : base(attacker, victim, null)
    {
        Damage = damage;
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "TakeDamage");
    }
}
