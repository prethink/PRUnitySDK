using System;

public class MultiplyDamageDecorator : IDamageProvider, IDamageModifier
{
    #region Поля и свойства

    private IDamageProvider damageProvider;

    private float multiply;

    private bool addCriticalFlag;

    #endregion

    #region IDamageProvider

    public DamageData GetDamageData()
    {
        var currentData = damageProvider.GetDamageData()?.Clone();

        if (currentData == null)
            return null;

        if (currentData.IsAppliedModifier(this))
            return currentData;

        if (currentData.RawDamage == 0f && currentData.Damage != 0f)
            currentData.RawDamage = currentData.Damage;

        if(addCriticalFlag)
            currentData.DamageType = currentData.DamageType | DamageType.Critical;

        currentData.Damage = currentData.Damage * multiply;
        currentData.AddModifier(this);

        return currentData;
    }

    #endregion

    #region IDamageModifier

    private readonly Guid identifier;

    public Guid ModifierIdentifier => identifier;

    public string ModifierName => nameof(MultiplyDamageDecorator);

    #endregion

    #region Конструкторы

    public MultiplyDamageDecorator(IDamageProvider damageProvider, float multiply, bool addCriticalFlag = true, Guid? modifierIdentifier = null)
    {
        this.damageProvider = damageProvider ?? throw new ArgumentNullException(nameof(damageProvider));
        if (multiply < 0f || float.IsNaN(multiply) || float.IsInfinity(multiply))
            throw new ArgumentOutOfRangeException(nameof(multiply));
        identifier = modifierIdentifier ?? Guid.NewGuid();
        this.multiply = multiply;
        this.addCriticalFlag = addCriticalFlag;
    }

    #endregion
}
