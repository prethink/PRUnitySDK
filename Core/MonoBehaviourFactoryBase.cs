using System;
using UnityEngine;

public abstract class MonoBehaviourFactoryBase<T> : IMonoBehaviourFactory 
    where T : MonoBehaviour
{
    public abstract string ResourcePath { get; }

    public abstract bool IsSingleton { get; }

    public abstract bool WorldPositionStays { get; }

    public abstract bool DonDestroyOnLoad { get; }

    private static T instance;

    public virtual T Create(Transform parent = null)
    {
        if (IsSingleton && instance != null)
            return instance;

        instance = UnityEngine.Object.Instantiate(Resources.Load<T>(ResourcePath));

        if(DonDestroyOnLoad)
            MonoBehaviour.DontDestroyOnLoad(instance);

        if (parent != null)
            instance.transform.SetParent(parent, WorldPositionStays);

        return instance;
    }
}

public abstract class SingletonMonoBehaviourFactoryBase<T> : MonoBehaviourFactoryBase<T> 
    where T : MonoBehaviour
{
    public override bool IsSingleton => true;

    public override bool WorldPositionStays => false;

    public override bool DonDestroyOnLoad => true;
}
