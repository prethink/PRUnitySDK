using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ProjectProperties : ICloneable
{
    /// <summary>
    /// Словарь для хранения универсальных объектов.
    /// </summary>
    [JsonProperty("obj")]
    public Dictionary<string, object> ObjectProperties = new();

    /// <summary>
    /// Словарь для хранения свойств типа int.
    /// </summary>
    [JsonProperty("long")]
    public Dictionary<string, long> LongProperties = new();

    /// <summary>
    /// Словарь для хранения свойств типа DateTime.
    /// </summary>
    [JsonProperty("DateTime")]
    public Dictionary<string, DateTime> DateTimeProperties = new();

    /// <summary>
    /// Словарь для хранения свойств типа string.
    /// </summary>
    [JsonProperty("str")]
    public Dictionary<string, string> StringProperties = new();

    /// <summary>
    /// Словарь для хранения свойств типа float.
    /// </summary>
    [JsonProperty("float")]
    public Dictionary<string, float> FloatProperties = new();

    /// <summary>
    /// Словарь для хранения свойств типа bool.
    /// </summary>
    [JsonProperty("bool")]
    public Dictionary<string, bool> BoolProperties = new();

    /// <summary>
    /// Создает глубокую копию объекта ProjectProperties.
    /// </summary>
    /// <returns>Глубокая копия объекта ProjectProperties.</returns>
    public object Clone()
    {
        var clone = new ProjectProperties
        {
            ObjectProperties = new Dictionary<string, object>(ObjectProperties), 
            LongProperties = new Dictionary<string, long>(LongProperties),
            DateTimeProperties = new Dictionary<string, DateTime>(DateTimeProperties),
            StringProperties = new Dictionary<string, string>(StringProperties),
            FloatProperties = new Dictionary<string, float>(FloatProperties),
            BoolProperties = new Dictionary<string, bool>(BoolProperties)
        };

        return clone;
    }

    public ProjectProperties()
    {
        ObjectProperties = new Dictionary<string, object>(ObjectProperties);
        LongProperties = new Dictionary<string, long>(LongProperties);
        DateTimeProperties = new Dictionary<string, DateTime>(DateTimeProperties);
        StringProperties = new Dictionary<string, string>(StringProperties);
        FloatProperties = new Dictionary<string, float>(FloatProperties);
        BoolProperties = new Dictionary<string, bool>(BoolProperties);
    }
}
