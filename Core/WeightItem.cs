using System;

[Serializable]
public record WeightItem<T>
{
    public T Item;
    public ulong Weight;
}



[Serializable]
public record WeightEntity<TEntity>() : WeightItem<TEntity>
    where TEntity : EntityBase { }
