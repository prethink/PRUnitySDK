using UnityEngine;

public static class ComponentExtension 
{
    public static T GetComponent<T>(this IComponent component) where T : class
    {
        if (component is Component comp)
            return comp.GetComponent<T>();

        return null;
    }
}
