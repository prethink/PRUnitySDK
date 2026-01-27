public abstract class CombatEventBase : GameplayEventArgsBase
{
    public IEntity Attacker { get; protected set; }

    public IEntity Victim { get; protected set; }

    public IWeapon? Weapon { get; protected set; }

    public CombatEventBase(IEntity attacker, IEntity victim, IWeapon weapon) 
    {
        Attacker = attacker;
        Victim = victim;
        Weapon = weapon;
    }

    public CombatEventBase(IEntity attacker, IEntity victim)
        : this(attacker, victim, null)
    {

    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Combat");
    }
}
