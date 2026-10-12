using System;
using UnityEngine;

public abstract class PRMonoBehaviourSingletonBase<T> : PRMonoBehaviour
    where T : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    protected static T instance;

    /// <summary>
    /// Экземпляр уже есть. В отличие от <see cref="Instance"/> ничего не создаёт.
    /// </summary>
    public static bool HasInstance => instance != null;

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

            // Обработчики на закрытии подняли бы новый объект, и он пережил бы сцену.
            if (instance == null && ApplicationLifetime.IsQuitting)
            {
                PRLog.WriteDebug(typeof(T), $"{typeof(T).Name} запрошен при закрытии приложения: не создаётся.");
                return null;
            }

            if (instance == null)
            {
                instance = CustomFactory != null 
                    ? MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(CustomFactory) 
                    : MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad<T>();
            }

            return instance;
        }
    }

    protected static MonoBehaviourFactoryBase<T> CustomFactory;

    public static void RegisterFactory(MonoBehaviourFactoryBase<T> factory)
    {
        PRUnitySDK.TrackInitialization<MonoBehaviourFactoryBase<T>>(
            factory?.GetType().Name ?? "<null>", PRInitializationCategory.Factory, () =>
            {
                CustomFactory = factory;
                return factory;
            });
    }
}
