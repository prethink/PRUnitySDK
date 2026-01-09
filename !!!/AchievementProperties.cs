using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class AchievementProperties : ICloneable
{
    /// <summary>
    /// Данные по ачивка.
    /// </summary>
    public Dictionary<string, long> AchievementData = new();

    /// <summary>
    /// Данные по триггерам.
    /// </summary>
    [JsonConverter(typeof(HashSetConverter<string>))]
    public HashSet<string> AchievementTriggerData = new();

    /// <summary>
    /// Данные по полученым наградам.
    /// </summary>
    [JsonConverter(typeof(HashSetConverter<Guid>))]
    public HashSet<Guid> RewardedItems = new();

    /// <summary>
    /// Данные завершенные.
    /// </summary>
    [JsonConverter(typeof(HashSetConverter<Guid>))]
    public HashSet<Guid> Completed = new();

    public object Clone()
    {
        return new AchievementProperties
        {
            AchievementData = new Dictionary<string, long>(AchievementData),
            AchievementTriggerData = new HashSet<string>(AchievementTriggerData),
            RewardedItems = new HashSet<Guid>(RewardedItems),
            Completed = new HashSet<Guid>(Completed)
        };
    }

    public AchievementProperties()
    {
        AchievementData = new();
        AchievementTriggerData = new();
        RewardedItems = new();
        Completed = new();
    }
}
