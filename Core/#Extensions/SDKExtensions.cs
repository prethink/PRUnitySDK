using UnityEngine;

public static class SDKExtensions
{
    public static T GetComponent<T>(this IEntity entity)
        where T : Component
    {
        if (entity?.gameObject == null)
            return null;

        return entity.gameObject.GetComponent<T>();
    }

    public static bool TryGetComponent<T>(this IEntity entity, out T component)
        where T : Component
    {
        if (entity?.gameObject == null)
        {
            component = null;
            return false;
        }

        return entity.gameObject.TryGetComponent(out component);
    }

    public static T GetComponentInChildren<T>(this IEntity entity, bool includeInactive = false)
        where T : Component
    {
        if (entity?.gameObject == null)
            return null;

        return entity.gameObject.GetComponentInChildren<T>(includeInactive);
    }

    public static T GetComponentInParent<T>(this IEntity entity)
        where T : Component
    {
        if (entity?.gameObject == null)
            return null;

        return entity.gameObject.GetComponentInParent<T>();
    }
}
