using UnityEngine;

public abstract class MonoBehaviourFactoryBase<T> : IMonoBehaviourFactory 
    where T : MonoBehaviour
{
    public abstract string ResourcePath { get; }

    public abstract bool IsSingleton { get; }

    public abstract bool WorldPositionStays { get; }

    private static T instance;

    public virtual T Create(Transform parent = null)
    {
        if (IsSingleton && instance != null)
            return instance;

        instance = Object.Instantiate(Resources.Load<T>(ResourcePath));
        if(parent != null)
        {
            instance.transform.SetParent(parent, WorldPositionStays);
        }
        return instance;
    }
}

public abstract class SingletonMonoBehaviourFactoryBase<T> : MonoBehaviourFactoryBase<T> 
    where T : MonoBehaviour
{
    public override bool IsSingleton => true;

    public override bool WorldPositionStays => false;
}
