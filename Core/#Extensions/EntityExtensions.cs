using UnityEngine;

/// <summary>
/// Набор расширений для удобного получения сущностей (Entity) из различных объектов Unity.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Пытается получить сущность из столкновения (Collision).
    /// </summary>
    /// <typeparam name="T">Тип сущности, наследуемый от EntityBase.</typeparam>
    /// <param name="collision">Объект столкновения.</param>
    /// <param name="entity">Найденная сущность.</param>
    /// <returns>True, если сущность успешно получена, иначе False.</returns>
    public static bool TryGetEntity<T>(this Collision collision, out T entity)
        where T : EntityBase
    {
        entity = null;
        if (collision.gameObject.TryGetComponent<EntityLink>(out var entityLink))
            return entityLink.TryGetEntity(out entity);

        return false;
    }

    /// <summary>
    /// Пытается получить сущность из коллайдера (Collider).
    /// </summary>
    /// <typeparam name="T">Тип сущности, наследуемый от EntityBase.</typeparam>
    /// <param name="collider">Коллайдер.</param>
    /// <param name="entity">Найденная сущность.</param>
    /// <returns>True, если сущность успешно получена, иначе False.</returns>
    public static bool TryGetEntity<T>(this Collider collider, out T entity)
        where T : EntityBase
    {
        entity = null;
        if (collider.gameObject.TryGetComponent<EntityLink>(out var entityLink))
            return entityLink.TryGetEntity(out entity);

        return false;
    }

    /// <summary>
    /// Пытается получить сущность из игрового объекта (GameObject).
    /// </summary>
    /// <typeparam name="T">Тип сущности, наследуемый от EntityBase.</typeparam>
    /// <param name="gameObject">Игровой объект.</param>
    /// <param name="entity">Найденная сущность.</param>
    /// <returns>True, если сущность успешно получена, иначе False.</returns>
    public static bool TryGetEntity<T>(this GameObject gameObject, out T entity)
        where T : EntityBase
    {
        entity = null;
        if (gameObject.TryGetComponent<EntityLink>(out var entityLink))
            return entityLink.TryGetEntity(out entity);

        return false;
    }

    /// <summary>
    /// Пытается получить сущность из объекта, если он является GameObject.
    /// </summary>
    /// <typeparam name="T">Тип сущности, наследуемый от EntityBase.</typeparam>
    /// <param name="obj">Произвольный объект.</param>
    /// <param name="entity">Найденная сущность.</param>
    /// <returns>True, если объект является GameObject и сущность успешно получена, иначе False.</returns>
    public static bool TryGetEntity<T>(this object obj, out T entity)
        where T : EntityBase
    {
        entity = null;
        if (obj is GameObject gameObject && gameObject.TryGetComponent<EntityLink>(out var entityLink))
            return entityLink.TryGetEntity(out entity);

        return false;
    }
}