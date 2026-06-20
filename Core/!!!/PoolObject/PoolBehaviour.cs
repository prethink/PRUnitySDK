using System;

public class PoolBehaviour 
{
    public bool InPool { get; protected set; } = false;
    public bool IsInitialize { get; protected set; }

    protected PoolObject poolObject;

    public event Action<bool> OnInitializeObject;

    public virtual void RegisterPoolObject(PoolObject poolObject)
    {
        this.poolObject = poolObject;
    }

    public virtual void InitializationPoolObject()
    {
        IsInitialize = true;
        var previousState = InPool;
        InPool = false;
        bool isFirstInitialize = previousState == InPool;
        OnInitializeObject?.Invoke(isFirstInitialize);
    }

    public virtual void OnDestroyPool(bool fullDestroy = false)
    {
        if (!fullDestroy)
            InPool = true;

        PRUnitySDK.Managers.ObjectPoolManager.OnObjectDestroy(poolObject, fullDestroy);
    }
}
