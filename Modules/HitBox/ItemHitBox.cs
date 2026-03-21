public class ItemHitBox : EntityHitBoxBase
{
    public override IDamageProvider GetHandledDamage(IDamageProvider damage)
    {
        return damage;
    }
}
