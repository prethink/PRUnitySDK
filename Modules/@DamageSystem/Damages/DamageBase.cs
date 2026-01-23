public abstract class DamageBase : IDamageProvider
{
    #region Поля и свойства

    public float Damage { get; protected set; }

    public float KnockBackPower { get; protected set; }

    public virtual DamageType DamageType { get; protected set; }

    private DamageData damageData;

    #endregion

    #region IDamageProvider

    public DamageData GetDamageData()
    {
        return damageData;
    }

    #endregion

    #region Конструкторы

    public DamageBase(DamageData damageData)
    {
        this.damageData = damageData;
    }

    public DamageBase(float damage) :this (damage, 0) { }

    public DamageBase(float damage, float knockBackPower) : this(damage, 0, DamageType.Generic) { }

    public DamageBase(float damage, float knockBackPower, DamageType damageType)
    {
        damageData = new DamageData()
        {
            Damage = damage,
            KnockBackPower = knockBackPower,
            DamageType = damageType,
        };
    }

    #endregion
}
