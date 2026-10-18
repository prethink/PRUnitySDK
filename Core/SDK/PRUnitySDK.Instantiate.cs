using UnityEngine;

public partial class PRUnitySDK
{
    public static T Instantiate<T>(T original)
        where T : Object
    {
        return Object.Instantiate(original);
    }

#if UNITY_6000_0_OR_NEWER
    public static T Instantiate<T>(
        T original,
        InstantiateParameters parameters)
        where T : Object
    {
        return Object.Instantiate(original, parameters);
    }
#endif

    public static T Instantiate<T>(
        T original,
        Transform parent)
        where T : Object
    {
        return Object.Instantiate(original, parent);
    }

    public static T Instantiate<T>(
        T original,
        Transform parent,
        bool worldPositionStays)
        where T : Object
    {
        return Object.Instantiate(original, parent, worldPositionStays);
    }

    public static T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation)
        where T : Object
    {
        return Object.Instantiate(original, position, rotation);
    }

#if UNITY_6000_0_OR_NEWER
    public static T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation,
        InstantiateParameters parameters)
        where T : Object
    {
        return Object.Instantiate(original, position, rotation, parameters);
    }
#endif

    public static T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
        where T : Object
    {
        return Object.Instantiate(original, position, rotation, parent);
    }
}
