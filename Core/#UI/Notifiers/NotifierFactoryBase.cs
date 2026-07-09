using UnityEngine;

public abstract class NotifierFactoryBase<T> : INotifierFactory 
    where T : MonoBehaviour
{
    public abstract string ResourcePath { get; }

    public virtual T Create()
    {
        var instance = Object.Instantiate(Resources.Load<T>(ResourcePath));
        instance.gameObject.transform.SetParent(PRUnitySDK.Windows.Notifiers.transform);
        return instance;
    }
}
