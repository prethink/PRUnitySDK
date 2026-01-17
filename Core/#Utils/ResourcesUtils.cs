using UnityEngine;

public static class ResourcesUtils 
{
    public static T LoadSingleFromResources<T>() 
        where T : ScriptableObject
    {
        var assets = Resources.LoadAll<T>(string.Empty);

        if (assets == null || assets.Length == 0)
            throw new ResourceNotFoundException(typeof(T));

        if (assets.Length > 1)
            throw new MultipleResourcesFoundException(typeof(T));

        return assets[0];
    }
}
