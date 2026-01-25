using UnityEngine;

public partial class PRUnitySDK
{
    public const string CorePrefabsPath = "Prefabs/Core";

    public static T Instantiate<T>(T original)
        where T : Object
    {
        return Object.Instantiate(original);
    }

    public static T Instantiate<T>(
        T original,
        InstantiateParameters parameters)
        where T : Object
    {
        return Object.Instantiate(original, parameters);
    }

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

    public static T Instantiate<T>(
        T original,
        Vector3 position,
        Quaternion rotation,
        InstantiateParameters parameters)
        where T : Object
    {
        return Object.Instantiate(original, position, rotation, parameters);
    }

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
