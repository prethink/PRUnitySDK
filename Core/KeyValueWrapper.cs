using System;
using UnityEngine;

[Serializable]
public class KeyValueWrapper<TKey, TValue>
{
    [SerializeField] private TKey key;
    [SerializeField] private TValue value;

    public TKey Key => key;
    public TValue Value => value;

    public KeyValueWrapper(TKey key, TValue value)
    {
        this.key = key;
        this.value = value;
    }
}
