using UnityEngine;

public static class MonoBehaviourUtils
{
    public static T CreateMonoBehaviourDontDestroyOnLoad<T>(string name = null) 
        where T : MonoBehaviour
    {
        GameObject obj = new GameObject();
        obj.name = string.IsNullOrEmpty(name) 
            ? typeof(T).Name 
            : name;

        T component = obj.AddComponent<T>();
        Object.DontDestroyOnLoad(obj);
        return component;
    }
}
