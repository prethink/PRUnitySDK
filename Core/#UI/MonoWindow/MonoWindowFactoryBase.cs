using Unity.VisualScripting;
using UnityEngine;

public abstract class MonoWindowFactoryBase<T> : IMonoWindowFactory 
    where T : MonoWindowBase
{
    public abstract bool UseSharedCanvas { get; }

    public abstract string ResourcePath { get; }

    public virtual T CreateMonoWindow()
    {
        var instance = Object.Instantiate(Resources.Load(ResourcePath));
        var parent = UseSharedCanvas
            ? PRUnitySDK.Windows.SharedCanvas.transform
            : PRUnitySDK.Windows.Container.transform;

        instance.GameObject().transform.SetParent(parent);
        return instance.GetComponent<T>();
    }
}
