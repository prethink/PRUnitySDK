using System;

public class PoolBehaviour 
{
    public bool InPool { get; protected set; } = false;
    public bool IsInitialize { get; protected set; }

    protected PoolObject poolObject;

    public event Action<bool> OnInitializeObject;

    /// <summary>
    /// Пул забрал объект в очередь сам, а не по просьбе объекта (<see cref="OnDestroyPool"/>).
    /// </summary>
    public event Action OnReturnedToPool;

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

    /// <summary>
    /// Отмечает, что объект лежит в очереди пула. Зовёт пул при каждом возврате.
    /// </summary>
    /// <remarks>
    /// Объект возвращают и мимо него самого: таймером жизни или прямым вызовом пула. Без отметки он считал себя
    /// выданным, и следующая выдача принималась за первую — без повторной инициализации. Если объект вернулся
    /// сам, отметка уже стоит и событие не поднимается.
    /// </remarks>
    public void MarkReturned()
    {
        if (InPool)
            return;

        InPool = true;
        OnReturnedToPool?.Invoke();
    }

    public virtual void OnDestroyPool(bool fullDestroy = false)
    {
        if (poolObject == null)
            return;

        if (!fullDestroy)
            InPool = true;

        PRUnitySDK.Managers.ObjectPool.OnObjectDestroy(poolObject, fullDestroy);
    }
}
