using UnityEngine;

public abstract class NotifierFactoryBase<T> : INotifierFactory 
    where T : NotifierBase
{
    public abstract string ResourcePath { get; }

    public abstract bool IsSingleton { get; }

    public bool WorldPositionStays => false;

    private static T instance;

    public virtual T Create()
    {
        if (IsSingleton && instance != null)
            return instance;

        PRUnitySDK.TrackInitialization<NotifierBase>(typeof(T).Name, PRInitializationCategory.Notifier, () =>
        {
            instance = Object.Instantiate(Resources.Load<T>(ResourcePath));
            instance.gameObject.transform.SetParent(PRUnitySDK.Windows.Notifiers.transform);
            return instance;
        });
        return instance;
    }
}
