public class PoolBehaviour 
{
    public bool InPool { get; protected set; }

    protected PoolObject poolObject;
    protected ObjectPoolSystem poolSystem;

    public virtual void RegisterPoolObject(PoolObject poolObject)
    {
        this.poolObject = poolObject;
    }

    public virtual void InitializationPoolObject()
    {
        InPool = false;
    }

    public virtual void OnDestroyPool(bool fullDestroy = false)
    {
        if (!fullDestroy)
            InPool = true;

        poolSystem.OnObjectDestroy(poolObject, fullDestroy);
    }

    public PoolBehaviour(ObjectPoolSystem poolSystem)
    {
        this.poolSystem = poolSystem;
    }
}
