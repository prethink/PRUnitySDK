using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class IIdentifiableItemConverter : JsonConverter<IIdentifiable>
{
    public override IIdentifiable ReadJson(JsonReader reader, Type objectType, IIdentifiable existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var selectableItem = new SelectableItem
        {
            Id = jsonObject["Id"]?.ToString()
        };
        return selectableItem;
    }

    public override void WriteJson(JsonWriter writer, IIdentifiable value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Id");
        writer.WriteValue(value.Id);
        writer.WriteEndObject();
    }
}