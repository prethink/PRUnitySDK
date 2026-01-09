using UnityEngine;

public static class MonoBehaviourUtils
{
    public static T CreateMonoBehaviourDontDestroyOnLoad<T>(string name = null) 
        where T : MonoBehaviour
    {
        GameObject obj = new GameObject();
        obj.name = $"☢ {typeof(T).Name}";

        T component = obj.AddComponent<T>();
        Object.DontDestroyOnLoad(obj);
        return component;
    }
}
