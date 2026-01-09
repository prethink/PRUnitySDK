using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class SelectableItemConverter : JsonConverter<ISelectableItem>
{
    public override ISelectableItem ReadJson(JsonReader reader, Type objectType, ISelectableItem existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var selectableItem = new SelectableItem
        {
            Id = jsonObject["Id"]?.ToString()
        };
        return selectableItem;
    }

    public override void WriteJson(JsonWriter writer, ISelectableItem value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Id");
        writer.WriteValue(value.Id);
        writer.WriteEndObject();
    }
}
