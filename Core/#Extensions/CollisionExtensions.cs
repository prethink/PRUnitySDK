using UnityEngine;

public static class CollisionExtensions
{
    public static bool TryGetEntity<T>(this Collision collision, out T entity) 
        where T : EntityBase
    {
        entity = null;
        if (collision.gameObject.TryGetComponent<EntityLink>(out var entityLink))
        {
            return entityLink.TryGetEntity(out entity);
        }
        return false;
    }

    public static bool TryGetEntity<T>(this Collider collider, out T entity) 
        where T : EntityBase
    {
        entity = null;
        if (collider.gameObject.TryGetComponent<EntityLink>(out var entityLink))
        {
            return entityLink.TryGetEntity(out entity);
        }
        return false;
    }
}
