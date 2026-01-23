using System;
using System.Collections.Generic;

public class RoundData
{
    private readonly Dictionary<string, object> data = new();

    public void Set<T>(string key, T value)
    {
        data[key] = value!;
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (data.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        return defaultValue!;
    }

    public bool HasKey(string key)
    {
        return data.ContainsKey(key);
    }

    public void SetOrAdd<T>(string key, Func<T> addFunc, Func<T, T> updateFunc)
    {
        if (data.TryGetValue(key, out var existing) && existing is T existingTyped)
            data[key] = updateFunc(existingTyped);
        else
            data[key] = addFunc();
    }

    public void Clear()
    {
        data.Clear();
    }
}