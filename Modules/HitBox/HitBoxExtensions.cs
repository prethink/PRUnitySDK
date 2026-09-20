using UnityEngine;

/// <summary>
/// Получение хитбокса и здоровья из того, с чем столкнулись.
/// </summary>
/// <remarks>
/// Прямой <c>TryGetComponent&lt;HealthComponent&gt;</c> на коллайдере работает только
/// у простой сущности, где здоровье и коллайдер лежат на одном объекте. У персонажа
/// коллайдер принадлежит хитбоксу, здоровье — самой сущности, и такая проверка молча
/// возвращает <c>false</c>: попадание есть, урона нет.
/// <para>
/// Родителей не обходим. Связь с сущностью задаётся явно — <see cref="EntityLinkBase"/>
/// на объекте коллайдера, — и это не прихоть: у вложенных сущностей (питомец внутри
/// игрока, предмет в руке) подъём по иерархии дотянулся бы до чужого здоровья.
/// </para>
/// </remarks>
public static class HitBoxExtensions
{
    /// <summary>
    /// Пытается получить хитбокс из коллайдера.
    /// Подходит для аргумента <c>OnTriggerEnter</c> и результатов <c>Raycast</c>.
    /// </summary>
    public static bool TryGetHitBox(this Collider collider, out EntityHitBoxBase hitBox)
    {
        return TryGetHitBox<EntityHitBoxBase>(collider, out hitBox);
    }

    /// <summary>
    /// Пытается получить хитбокс указанного типа из коллайдера.
    /// </summary>
    /// <typeparam name="T">Тип хитбокса: <see cref="UnitHitBox"/>, <see cref="ItemHitBox"/> или их наследник.</typeparam>
    public static bool TryGetHitBox<T>(this Collider collider, out T hitBox)
        where T : EntityHitBoxBase
    {
        hitBox = null;

        return collider != null && TryGetHitBox(collider.gameObject, out hitBox);
    }

    /// <summary>
    /// Пытается получить хитбокс из столкновения.
    /// </summary>
    public static bool TryGetHitBox(this Collision collision, out EntityHitBoxBase hitBox)
    {
        return TryGetHitBox<EntityHitBoxBase>(collision, out hitBox);
    }

    /// <summary>
    /// Пытается получить хитбокс указанного типа из столкновения.
    /// </summary>
    /// <typeparam name="T">Тип хитбокса.</typeparam>
    public static bool TryGetHitBox<T>(this Collision collision, out T hitBox)
        where T : EntityHitBoxBase
    {
        hitBox = null;

        if (collision == null)
            return false;

        // Сначала по задетому коллайдеру: у составного тела зоны разные, и голова
        // от ноги отличается именно им. Объект столкновения на такой иерархии —
        // это тело целиком, по нему зону не различить.
        if (TryGetHitBox(collision.collider, out hitBox))
            return true;

        return TryGetHitBox(collision.gameObject, out hitBox);
    }

    /// <summary>
    /// Пытается получить хитбокс с игрового объекта.
    /// </summary>
    public static bool TryGetHitBox(this GameObject gameObject, out EntityHitBoxBase hitBox)
    {
        return TryGetHitBox<EntityHitBoxBase>(gameObject, out hitBox);
    }

    /// <summary>
    /// Пытается получить хитбокс указанного типа с игрового объекта.
    /// </summary>
    /// <remarks>
    /// Только со своего объекта, без подъёма к <c>Rigidbody</c>: хитбокс — это зона
    /// конкретного коллайдера, и взятый с тела он приписал бы попадание не туда.
    /// </remarks>
    /// <typeparam name="T">Тип хитбокса.</typeparam>
    public static bool TryGetHitBox<T>(this GameObject gameObject, out T hitBox)
        where T : EntityHitBoxBase
    {
        hitBox = null;

        return gameObject != null && gameObject.TryGetComponent(out hitBox);
    }

    /// <summary>
    /// Пытается получить здоровье сущности из коллайдера.
    /// Подходит для аргумента <c>OnTriggerEnter</c> и результатов <c>Raycast</c>.
    /// </summary>
    /// <remarks>
    /// Здоровье возвращается и у хитбокса, но бить лучше в сам хитбокс: он умножает урон
    /// на зону и помечает крит, а напрямую в здоровье эти правила проходят мимо.
    /// </remarks>
    public static bool TryGetHealth(this Collider collider, out HealthComponent health)
    {
        health = null;

        if (collider == null)
            return false;

        if (TryGetHealth(collider.gameObject, out health))
            return true;

        // У составного тела коллайдер часто висит на дочернем объекте, а связь
        // с сущностью — на объекте с Rigidbody.
        GameObject body = collider.attachedRigidbody != null
            ? collider.attachedRigidbody.gameObject
            : null;

        return body != null && body != collider.gameObject && TryGetHealth(body, out health);
    }

    /// <summary>
    /// Пытается получить здоровье сущности из столкновения.
    /// </summary>
    public static bool TryGetHealth(this Collision collision, out HealthComponent health)
    {
        health = null;

        if (collision == null)
            return false;

        // По задетому коллайдеру, потом по объекту столкновения: у составного тела
        // связь с сущностью бывает и там, и там.
        if (TryGetHealth(collision.collider, out health))
            return true;

        return TryGetHealth(collision.gameObject, out health);
    }

    /// <summary>
    /// Пытается получить здоровье сущности с игрового объекта.
    /// </summary>
    /// <remarks>
    /// Сначала своё — у простой сущности здоровье лежит там же, где коллайдер. Потом
    /// через связь с сущностью: так отвечают и хитбокс (ему <c>EntityLink</c> обязателен),
    /// и любой помеченный коллайдер.
    /// <para>
    /// Связь берётся базовым типом, а не <c>EntityLink</c>: у игрока на теле висит
    /// <c>PlayerLink</c>, и по конкретному типу он бы не нашёлся.
    /// </para>
    /// </remarks>
    public static bool TryGetHealth(this GameObject gameObject, out HealthComponent health)
    {
        health = null;

        if (gameObject == null)
            return false;

        if (gameObject.TryGetComponent(out health))
            return true;

        if (!gameObject.TryGetComponent<EntityLinkBase>(out var link) || link.Entity == null)
            return false;

        return link.Entity.TryGetComponent(out health);
    }
}
