using System;

/// <summary>
/// Правило урона «свой-чужой»: применяет к удару ответ <see cref="EntitySides.GetDamage"/>.
/// </summary>
/// <remarks>
/// SDK ставит его сам на старте: в список правил проекта его класть не нужно. Выключенные
/// стороны (<see cref="EntitySidesSettings.Enabled"/>) правило пропускает, поэтому их можно
/// включать и выключать на ходу.
/// Через правила урона, а не проверкой в оружии, — так ответ получают любые удары:
/// касание, ближний бой, снаряд, взрыв.
/// </remarks>
[Serializable]
public sealed class EntitySideDamageRule : DamageRule
{
    [NonSerialized]
    private Guid modifierIdentifier = Guid.NewGuid();

    public EntitySideDamageRule()
    {
        // Отказ в ударе принято ставить раньше правок урона.
        Order = -200;
    }

    /// <inheritdoc />
    protected override void Apply(DamageHookEvent eventArgs, IHookListener source)
    {
        switch (EntitySides.GetDamage(eventArgs.Attacker, eventArgs.Victim))
        {
            case EntitySideDamage.Block:
                eventArgs.BlockDamage(source);
                break;

            case EntitySideDamage.NoDamage:
                if (modifierIdentifier == Guid.Empty)
                    modifierIdentifier = Guid.NewGuid();

                eventArgs.ModifyDamage(
                    source,
                    new MultiplyDamageDecorator(eventArgs.DamageProvider, 0f, false, modifierIdentifier));
                break;
        }
    }
}
