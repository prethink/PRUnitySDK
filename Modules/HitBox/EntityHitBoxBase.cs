using UnityEngine;

/// <summary>
/// Базовый компонент физической зоны попадания, связанной с сущностью.
/// </summary>
[RequireComponent(typeof(EntityLink))]
public abstract class EntityHitBoxBase : PRMonoBehaviour, IDamageable
{
    /// <summary>
    /// Связь хитбокса с сущностью-владельцем.
    /// </summary>
    [field: SerializeField] public EntityLink EntityLink { get; private set; }

    /// <summary>
    /// Коллайдер, представляющий эту зону попадания.
    /// </summary>
    [field: SerializeField] public Collider Collider { get; private set; }

    /// <summary>
    /// Настроены ли ссылка на сущность и коллайдер.
    /// </summary>
    public bool IsConfigured => EntityLink != null &&
                                EntityLink.Entity != null &&
                                Collider != null;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        FindComponents();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        FindComponents();
    }

    /// <summary>
    /// Передаёт обычное попадание в компонент здоровья связанной сущности.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="weapon">Использованное оружие.</param>
    /// <param name="damage">Провайдер исходного урона.</param>
    /// <returns>Результат обработки либо <see cref="DamageResult.NotHandled"/>.</returns>
    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage)
    {
        if (damage != null && TryGetHealthComponent(out var healthComponent))
            return healthComponent.TakeDamage(attacker, weapon, GetHandledDamage(damage));

        return DamageResult.NotHandled;
    }

    /// <summary>
    /// Передаёт попадание с мировой точкой в компонент здоровья.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="weapon">Использованное оружие.</param>
    /// <param name="damage">Провайдер исходного урона.</param>
    /// <param name="point">Мировая точка попадания.</param>
    /// <returns>Результат обработки либо <see cref="DamageResult.NotHandled"/>.</returns>
    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point)
    {
        if (damage != null && TryGetHealthComponent(out var healthComponent))
            return healthComponent.TakeDamage(attacker, weapon, GetHandledDamage(damage), point);

        return DamageResult.NotHandled;
    }

    /// <summary>
    /// Передаёт попадание с конкретным коллайдером в компонент здоровья.
    /// </summary>
    /// <param name="attacker">Атакующая сущность.</param>
    /// <param name="weapon">Использованное оружие.</param>
    /// <param name="damage">Провайдер исходного урона.</param>
    /// <param name="collider">Коллайдер попадания; при <c>null</c> используется <see cref="Collider"/>.</param>
    /// <returns>Результат обработки либо <see cref="DamageResult.NotHandled"/>.</returns>
    public virtual DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider)
    {
        if (damage != null && TryGetHealthComponent(out var healthComponent))
            return healthComponent.TakeDamage(
                attacker,
                weapon,
                GetHandledDamage(damage),
                collider != null ? collider : Collider);

        return DamageResult.NotHandled;
    }

    /// <summary>
    /// Преобразует урон с учётом особенностей конкретного хитбокса.
    /// </summary>
    /// <param name="damage">Исходный провайдер урона.</param>
    /// <returns>Провайдер обработанного урона.</returns>
    public abstract IDamageProvider GetHandledDamage(IDamageProvider damage);

    private void FindComponents()
    {
        EntityLink ??= GetComponent<EntityLink>();
        Collider ??= GetComponent<Collider>();
    }

    private bool TryGetHealthComponent(out HealthComponent healthComponent)
    {
        healthComponent = null;
        return EntityLink != null &&
               EntityLink.Entity != null &&
               EntityLink.Entity.TryGetComponent(out healthComponent);
    }
}
