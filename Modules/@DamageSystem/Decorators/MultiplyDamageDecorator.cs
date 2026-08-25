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
        var currentData = damageProvider.GetDamageData().Clone();

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

    private static readonly Guid Identifier = new Guid("6c02086d-f022-49d3-9e84-9678e40e3e88");

    public Guid ModifierIdentifier => Identifier;

    public string ModifierName => nameof(MultiplyDamageDecorator);

    #endregion

    #region Конструкторы

    public MultiplyDamageDecorator(IDamageProvider damageProvider, float multiply, bool addCriticalFlag = true)
    {
        this.damageProvider = damageProvider;
        this.multiply = multiply;
        this.addCriticalFlag = addCriticalFlag;
    }

    #endregion
}
