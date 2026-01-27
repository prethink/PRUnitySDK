public class KillEvent : CombatEventBase
{
    public KillEvent(IEntity attacker, IEntity victim, IWeapon weapon) 
        : base(attacker, victim, weapon)
    {
    }

    public KillEvent(IEntity attacker, IEntity victim) 
        : base(attacker, victim, null)
    {
        
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Kill");
    }
}
