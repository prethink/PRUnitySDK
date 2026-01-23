using UnityEngine;

/// <summary>
/// Интерфейс для объектов, которые могут получать урон.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Наносит урон сущности.
    /// </summary>
    /// <param name="attacker">Сущность, наносящая урон.</param>
    /// <param name="weapon">Оружие, которым нанесён урон (может быть null).</param>
    /// <param name="damage">Информация об уроне (значение, тип, крит и т.д.).</param>
    /// <returns>Результат обработки урона (например, финальное значение урона, был ли крит и т.п.).</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage);

    /// <summary>
    /// Наносит урон сущности в определённой точке мира.
    /// </summary>
    /// <param name="attacker">Сущность, наносящая урон.</param>
    /// <param name="weapon">Оружие, которым нанесён урон (может быть null).</param>
    /// <param name="damage">Информация об уроне (значение, тип, крит и т.д.).</param>
    /// <param name="point">Точка в мировых координатах, куда пришёлся урон (например, попадание пули).</param>
    /// <returns>Результат обработки урона.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point);

    /// <summary>
    /// Наносит урон сущности в определённый коллайдер.
    /// </summary>
    /// <param name="attacker">Сущность, наносящая урон.</param>
    /// <param name="weapon">Оружие, которым нанесён урон (может быть null).</param>
    /// <param name="damage">Информация об уроне (значение, тип, крит и т.д.).</param>
    /// <param name="collider">Коллайдер, по которому пришёлся урон (например, часть тела или объект брони).</param>
    /// <returns>Результат обработки урона.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider);
}
