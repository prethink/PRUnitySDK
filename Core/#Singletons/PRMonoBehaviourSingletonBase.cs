using UnityEngine;

public abstract class PRMonoBehaviourSingletonBase<T> : PRMonoBehaviour
    where T : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    protected static T instance;

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindObjectOfType<T>();

            if (instance == null)
                instance = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad<T>();

            return instance;
        }
    }
}
