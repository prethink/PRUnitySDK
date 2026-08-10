using System;

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
        this.damageData = damageData ?? throw new ArgumentNullException(nameof(damageData));
        if (this.damageData.RawDamage == 0f && this.damageData.Damage != 0f)
            this.damageData.RawDamage = this.damageData.Damage;

        Damage = damageData.Damage;
        KnockBackPower = damageData.KnockBackPower;
        DamageType = damageData.DamageType;
    }

    public DamageBase(float damage) :this (damage, 0) { }

    public DamageBase(float damage, float knockBackPower) : this(damage, knockBackPower, DamageType.Generic) { }

    public DamageBase(float damage, float knockBackPower, DamageType damageType)
    {
        damageData = new DamageData()
        {
            DamageId = Guid.NewGuid(),
            Damage = damage,
            RawDamage = damage,
            KnockBackPower = knockBackPower,
            DamageType = damageType,
        };

        Damage = damage;
        KnockBackPower = knockBackPower;
        DamageType = damageType;
    }

    #endregion
}
