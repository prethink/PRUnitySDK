using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public class HashSetConverter<T> : JsonConverter<HashSet<T>>
{
    public override HashSet<T> ReadJson(JsonReader reader, Type objectType, HashSet<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var list = serializer.Deserialize<List<T>>(reader);
        return list != null ? new HashSet<T>(list) : new HashSet<T>();
    }

    public override void WriteJson(JsonWriter writer, HashSet<T> value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.ToList());
    }
}
