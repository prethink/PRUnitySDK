public class EntityKillEventArgs : CombatEventBase
{
    public EntityKillEventArgs(IEntity attacker, IEntity victim, IWeapon weapon) 
        : base(attacker, victim, weapon)
    {
    }

    public EntityKillEventArgs(IEntity attacker, IEntity victim) 
        : base(attacker, victim, null)
    {
        
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Kill");
    }
}
