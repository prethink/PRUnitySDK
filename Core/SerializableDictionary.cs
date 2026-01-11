using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new();
    [SerializeField] private List<TValue> values = new();

    private Dictionary<TKey, TValue> dictionary = new();

    public IReadOnlyDictionary<TKey, TValue> Dictionary => dictionary;

    public TValue this[TKey key]
    {
        get => dictionary[key];
        set => dictionary[key] = value;
    }


    public bool TryGetValue(TKey key, out TValue value)
        => dictionary.TryGetValue(key, out value);

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        foreach (var pair in dictionary)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        dictionary.Clear();

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
    }
}

[Serializable]
public class TranslateDictionary : SerializableDictionary<LangType, string>
{
}