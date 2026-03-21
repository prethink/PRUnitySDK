

using System.Drawing;
using UnityEngine;

[RequireComponent(typeof(EntityLink))]
public abstract class EntityHitBoxBase : PRMonoBehaviour, IDamageable
{
    [field: SerializeField] public EntityLink EntityLink { get; private set; }
    [field: SerializeField] public Collider Collider { get; private set; }

    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage)
    {
        if(attacker.TryGetComponent<HealthComponent>(out var healthComponent))
            return healthComponent.TakeDamage(attacker, weapon, damage);

        return DamageResult.NotHandled;
    }

    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point)
    {
        if (attacker.TryGetComponent<HealthComponent>(out var healthComponent))
            return healthComponent.TakeDamage(attacker, weapon, damage, point);

        return DamageResult.NotHandled;
    }

    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider)
    {
        if (attacker.TryGetComponent<HealthComponent>(out var healthComponent))
            return healthComponent.TakeDamage(attacker, weapon, damage, collider);

        return DamageResult.NotHandled;
    }

    public abstract IDamageProvider GetHandledDamage(IDamageProvider damage);
}
