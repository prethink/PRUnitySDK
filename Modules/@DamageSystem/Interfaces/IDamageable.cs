using UnityEngine;

/// <summary>
/// Объект, способный принять урон.
/// <para>
/// Реализуют как носители здоровья (<see cref="HealthComponent"/>), так и прокси -
/// например, хитбоксы: они не хранят здоровье, а добавляют к урону зону попадания
/// и передают его владельцу.
/// </para>
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Принять урон без сведений о месте попадания.
    /// </summary>
    /// <param name="attacker">Кто наносит урон. Может быть null для урона от окружения.</param>
    /// <param name="weapon">Чем нанесён урон. Может быть null.</param>
    /// <param name="damage">Источник данных об уроне; проходит через декораторы и хуки.</param>
    /// <returns>Чем закончилась попытка.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage);

    /// <summary>
    /// Принять урон с известной точкой попадания - она попадает в
    /// <see cref="DamageOutcome.HitPoint"/> и используется для эффектов и отброса.
    /// </summary>
    /// <param name="attacker">Кто наносит урон.</param>
    /// <param name="weapon">Чем нанесён урон.</param>
    /// <param name="damage">Источник данных об уроне.</param>
    /// <param name="point">Мировая точка попадания.</param>
    /// <returns>Чем закончилась попытка.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point);

    /// <summary>
    /// Принять урон с известным коллайдером - по нему определяется зона попадания,
    /// когда у сущности несколько хитбоксов.
    /// </summary>
    /// <param name="attacker">Кто наносит урон.</param>
    /// <param name="weapon">Чем нанесён урон.</param>
    /// <param name="damage">Источник данных об уроне.</param>
    /// <param name="collider">Задетый коллайдер.</param>
    /// <returns>Чем закончилась попытка.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider);
}
