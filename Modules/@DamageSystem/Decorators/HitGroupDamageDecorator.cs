using System;

/// <summary>
/// Добавляет к урону зону попадания и применяет её множитель.
/// </summary>
public sealed class HitGroupDamageDecorator : IDamageProvider, IDamageModifier
{
    private readonly IDamageProvider damageProvider;
    private readonly HitGroup hitGroup;
    private readonly float multiplier;
    private readonly bool critical;

    /// <summary>
    /// Стабильный идентификатор зонального модификатора.
    /// </summary>
    private static readonly Guid Identifier = new Guid("5e2cc760-3847-4862-b7b4-161481331796");

    public Guid ModifierIdentifier => Identifier;

    /// <summary>
    /// Читаемое имя модификатора.
    /// </summary>
    public string ModifierName => nameof(HitGroupDamageDecorator);

    /// <summary>
    /// Создаёт модификатор зоны попадания.
    /// </summary>
    /// <param name="damageProvider">Исходный провайдер урона.</param>
    /// <param name="hitGroup">Зона попадания.</param>
    /// <param name="multiplier">Неотрицательный множитель урона.</param>
    /// <param name="critical">Нужно ли добавить флаг критического урона.</param>
    public HitGroupDamageDecorator(
        IDamageProvider damageProvider,
        HitGroup hitGroup,
        float multiplier,
        bool critical = false)
    {
        this.damageProvider = damageProvider ?? throw new ArgumentNullException(nameof(damageProvider));
        this.hitGroup = hitGroup;
        this.multiplier = Math.Max(0f, multiplier);
        this.critical = critical;
    }

    /// <summary>
    /// Возвращает копию данных с применённой зоной и множителем.
    /// </summary>
    /// <returns>Модифицированные данные урона.</returns>
    public DamageData GetDamageData()
    {
        var data = damageProvider.GetDamageData().Clone();
        if (data.IsAppliedModifier(this))
            return data;

        if (data.RawDamage == 0f && data.Damage != 0f)
            data.RawDamage = data.Damage;

        data.HitGroup = hitGroup;
        data.Damage *= multiplier;

        if (critical)
            data.DamageType |= DamageType.Critical;

        data.AddModifier(this);
        return data;
    }
}
