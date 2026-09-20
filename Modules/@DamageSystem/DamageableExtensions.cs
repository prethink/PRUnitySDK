using UnityEngine;

/// <summary>
/// Нанесение урона без оружия.
/// </summary>
/// <remarks>
/// Оружие в системе урона необязательно: <see cref="IDamageable.TakeDamage(IEntity, IWeapon, IDamageProvider)"/>
/// принимает <c>null</c>, и конвейер его нигде не разыменовывает — <c>IWeapon</c> только
/// доезжает до <see cref="DamageHookEvent.Weapon"/> и событий как пометка «чем ударили».
/// <para>
/// Расширения нужны ради читаемости вызова. <c>TakeDamage(attacker, null, damage)</c>
/// выглядит как забытый аргумент, а не как «оружия здесь нет»: шипы, лава, падение,
/// отравление и таран сущностью происходят сами по себе, и предъявить им нечего.
/// </para>
/// <para>
/// Когда ударивший предмет всё же нужно назвать отдельно от атакующего — брошенный
/// камень, ловушка с именем в списке убийств, — заводят реализацию <see cref="IWeapon"/>:
/// она и есть то место, где такое имя живёт.
/// </para>
/// </remarks>
public static class DamageableExtensions
{
    /// <summary>
    /// Наносит урон без оружия.
    /// </summary>
    /// <param name="target">Кому достаётся урон.</param>
    /// <param name="attacker">Кто наносит урон; может быть <c>null</c> для урона от окружения.</param>
    /// <param name="damage">Источник данных об уроне.</param>
    /// <returns>Подробный итог попытки; никогда не <c>null</c>.</returns>
    public static DamageOutcome TakeDamage(this IDamageable target, IEntity attacker, IDamageProvider damage)
    {
        return target.TakeDamage(attacker, null, damage);
    }

    /// <summary>
    /// Наносит урон без оружия, с известной точкой попадания.
    /// </summary>
    /// <param name="target">Кому достаётся урон.</param>
    /// <param name="attacker">Кто наносит урон.</param>
    /// <param name="damage">Источник данных об уроне.</param>
    /// <param name="point">Мировая точка попадания: уходит в отброс и эффекты.</param>
    /// <returns>Подробный итог попытки; никогда не <c>null</c>.</returns>
    public static DamageOutcome TakeDamage(this IDamageable target, IEntity attacker, IDamageProvider damage, Vector3 point)
    {
        return target.TakeDamage(attacker, null, damage, point);
    }

    /// <summary>
    /// Наносит урон без оружия, с известным коллайдером попадания.
    /// </summary>
    /// <param name="target">Кому достаётся урон.</param>
    /// <param name="attacker">Кто наносит урон.</param>
    /// <param name="damage">Источник данных об уроне.</param>
    /// <param name="collider">Задетый коллайдер: по нему определяется зона.</param>
    /// <returns>Подробный итог попытки; никогда не <c>null</c>.</returns>
    public static DamageOutcome TakeDamage(this IDamageable target, IEntity attacker, IDamageProvider damage, Collider collider)
    {
        return target.TakeDamage(attacker, null, damage, collider);
    }
}
