using UnityEngine;

public abstract class MonoBehaviourSingletonBase<T> : MonoBehaviour
    where T : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    protected static T instance;

    /// <summary>
    /// Экземпляр уже существует.
    /// </summary>
    /// <remarks>
    /// В отличие от <see cref="Instance"/> ничего не создаёт. Нужно тем, кто работает
    /// на уничтожении объектов: обращение к <see cref="Instance"/> в этот момент подняло бы
    /// менеджер заново, и после выхода из игры на сцене остался бы лишний объект.
    /// </remarks>
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
