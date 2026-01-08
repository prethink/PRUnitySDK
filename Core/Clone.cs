using UnityEngine;

public class Clone : MonoBehaviour
{
    public T Copy<T>(Vector3 position, Transform parent) 
        where T : Component
    {
        Clone instance = Instantiate(this);

        if(parent != null)
            instance.transform.SetParent(parent);

        instance.transform.localPosition = position;

        return instance.GetComponent<T>();
    }

    public T Copy<T>(Vector3 position)
        where T : Component
    {
        return Copy<T>(position, null);
    }

    public T Copy<T>(Transform parent, bool setParent = true)
        where T : Component
    {
        return Copy<T>(parent.position, setParent ? parent : null);
    }
}