public class DamageHookEvent : HookEventArgsBase
{
    public DamageResult DamageResult { get; set; }
    public IDamageProvider DamageProvider { get; set; }
    public IEntity Attacker { get; }
    public IEntity Victim { get; }
    public IWeapon Weapon { get; }

    public DamageHookEvent(IEntity attacker, IWeapon weapon, IEntity victim, IDamageProvider damageProvider, DamageResult damageResult)
    {
        this.Attacker = attacker;
        this.Victim = victim;
        this.DamageProvider = damageProvider;
        this.DamageResult = damageResult;
        this.Weapon = weapon;
    }

    public void ModifyDamage(IHookListener modifier, IDamageProvider damage)
    {
        DamageProvider = damage;
        Modify(modifier);
    }

    public void ModifyResult(IHookListener modifier, DamageResult damageResult)
    {
        DamageResult = damageResult;
        Modify(modifier);
    }

    public void BlockDamage(IHookListener modifier)
    {
        DamageResult = DamageResult.Blocked;
        Modify(modifier);
        Result = HookResult.HandledMain;
    }

    public void MissDamage(IHookListener modifier)
    {
        DamageResult = DamageResult.Miss;
        Modify(modifier);
        Result = HookResult.HandledMain;
    }
}
