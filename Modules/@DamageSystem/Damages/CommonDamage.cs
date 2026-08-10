public class CommonDamage : DamageBase
{
    public CommonDamage(DamageData damageData) : base(damageData)
    {
    }

    public CommonDamage(float damage) : base(damage)
    {
    }

    public CommonDamage(float damage, float knockBackPower) : base(damage, knockBackPower)
    {
    }

    public CommonDamage(float damage, float knockBackPower, DamageType damageType)
        : base(damage, knockBackPower, damageType)
    {
    }
}
