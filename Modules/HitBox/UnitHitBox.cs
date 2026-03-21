public class UnitHitBox : EntityHitBoxBase
{
    public override IDamageProvider GetHandledDamage(IDamageProvider damage)
    {
        return damage;
    }
}
