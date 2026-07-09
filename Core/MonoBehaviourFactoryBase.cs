using UnityEngine;

public abstract class MonoBehaviourFactoryBase<T> : IMonoBehaviourFactory 
    where T : MonoBehaviour
{
    public abstract string ResourcePath { get; }

    public virtual T Create()
    {
        return Object.Instantiate(Resources.Load<T>(ResourcePath));
    }
}
