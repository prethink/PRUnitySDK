using System;
using UnityEngine;

/// <summary>
/// Правило урона: умножает урон подходящих ударов или отклоняет их.
/// </summary>
/// <remarks>
/// Множитель ноль — удар без урона: попадание засчитано, эффекты играют, цель цела.
/// Отклонение (<see cref="DamageRuleAction.Block"/>) — удара не было вовсе. Урон
/// оборачивается декоратором, а не переписывается числом, поэтому по цепочке видно,
/// кто и на сколько его изменил.
/// </remarks>
[Serializable]
public class MultiplyDamageRule : DamageRule
{
    [SerializeField]
    [Tooltip("Multiply - умножить урон, Block - отклонить удар.")]
    private DamageRuleAction action = DamageRuleAction.Multiply;

    [SerializeField, Min(0f)]
    [Tooltip("Во сколько раз множится урон. 0 - удар без урона.")]
    private float multiplier;

    [SerializeField]
    [Tooltip("Считать изменённый удар критическим.")]
    private bool markAsCritical;

    [NonSerialized]
    private Guid modifierIdentifier = Guid.NewGuid();

    public MultiplyDamageRule()
    {
    }

    /// <param name="multiplier">Множитель урона; ноль — удар без урона.</param>
    public MultiplyDamageRule(float multiplier)
    {
        this.multiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// Правило, которое отклоняет подходящие удары.
    /// </summary>
    public static MultiplyDamageRule CreateBlock()
    {
        return new MultiplyDamageRule { action = DamageRuleAction.Block, Order = -200 };
    }

    /// <inheritdoc />
    protected override void Apply(DamageHookEvent eventArgs, IHookListener source)
    {
        // Объект, пришедший из сериализации, конструктор не проходит — идентификатор
        // дописывается при первом ударе.
        if (modifierIdentifier == Guid.Empty)
            modifierIdentifier = Guid.NewGuid();

        if (action == DamageRuleAction.Block)
        {
            eventArgs.BlockDamage(source);
            return;
        }

        eventArgs.ModifyDamage(
            source,
            new MultiplyDamageDecorator(eventArgs.DamageProvider, multiplier, markAsCritical, modifierIdentifier));
    }
}
