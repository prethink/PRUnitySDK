using Unity.VisualScripting;
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

        instance = Object.Instantiate(Resources.Load<T>(ResourcePath));

        var parent = UseSharedCanvas
            ? PRUnitySDK.Windows.SharedCanvas.transform
            : PRUnitySDK.Windows.Container.transform;

        instance.GameObject().transform.SetParent(parent, WorldPositionStays);
        return instance.GetComponent<T>();
    }
}
