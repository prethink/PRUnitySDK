using UnityEngine;

public abstract class MonoWindowFactoryBase<T> : IMonoWindowFactory 
    where T : MonoWindowBase
{
    public abstract bool UseSharedCanvas { get; }
    public abstract bool WorldPositionStays { get; }

    public abstract string ResourcePath { get; }

    public abstract bool IsSingleton { get; }

    private static T instance;

    public virtual T CreateMonoWindow()
    {
        if (IsSingleton && instance != null)
            return instance;

        if (string.IsNullOrWhiteSpace(ResourcePath))
        {
            PRLog.WriteError(GetType(), "ResourcePath для MonoWindow не может быть пустым.");
            return null;
        }

        T prefab = Resources.Load<T>(ResourcePath);
        if (prefab == null)
        {
            PRLog.WriteError(GetType(),
                $"Не найден prefab MonoWindow типа '{typeof(T).Name}' по пути Resources/{ResourcePath}.");
            return null;
        }

        var parent = UseSharedCanvas
            ? PRUnitySDK.Windows.SharedCanvas?.transform
            : PRUnitySDK.Windows.Container?.transform;

        if (parent == null)
        {
            PRLog.WriteError(GetType(),
                $"Невозможно создать MonoWindow '{typeof(T).Name}': контейнер окон ещё не инициализирован.");
            return null;
        }

        T createdWindow = null;
        PRUnitySDK.TrackInitialization<MonoWindowBase>(typeof(T).Name, PRInitializationCategory.MonoWindow,
            () => createdWindow = Object.Instantiate(prefab, parent, WorldPositionStays));
        if (IsSingleton)
            instance = createdWindow;

        return createdWindow;
    }
}
