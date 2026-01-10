using UnityEngine;

public static class MonoBehaviourUtils
{
    public static T CreateMonoBehaviourDontDestroyOnLoad<T>(string name = null, bool useRadiationIcon = true) 
        where T : MonoBehaviour
    {
        GameObject obj = new GameObject();
        obj.name = $"{(useRadiationIcon ? "☢ " : "")}{(string.IsNullOrEmpty(name) ? typeof(T).Name : name)}";

        T component = obj.AddComponent<T>();
        Object.DontDestroyOnLoad(obj);
        return component;
    }

    public static PRContainer CreateContainer(string name)
    {
        return CreateMonoBehaviourDontDestroyOnLoad<PRContainer>("▣ " + name, false);
    }
}
