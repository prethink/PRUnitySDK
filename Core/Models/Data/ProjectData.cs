using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public partial class ProjectData : ICloneable
{
    #region Поля и свойства

    #region Общие свойства

    ///// <summary>
    ///// Награды за очки в конце уровня.
    ///// </summary>
    //public List<RewardForPointData> RewardForPoints = new();

    ///// <summary>
    ///// Данные получения ежедневных подарков.
    ///// </summary>
    //public List<DailyGiftData> DailyGiftData = new();

    /// <summary>
    /// Данные инвентаря.
    /// </summary>
    public InventoryData InventoryData = new();

    ///// <summary>
    ///// Идентификаторы выбранных вещей.
    ///// </summary>
    //public List<PlayerSelectedData> SelectedPlayerItem = new();

    ///// <summary>
    ///// Идентификаторы открытых предметов.
    ///// </summary>
    //public List<ItemStack> OpenedItems = new();

    ///// <summary>
    ///// Список открытых уровней.
    ///// </summary>
    //[JsonConverter(typeof(HashSetConverter<int>))]
    //public HashSet<int> OpenMaps = new();

    ///// <summary>
    ///// Идентификаторы покупок.
    ///// </summary>
    //public List<string> Purchases = new();

    /// <summary>
    /// Свойства проекта.
    /// </summary>
    public ProjectProperties ProjectProperties;

    ///// <summary>
    ///// Свойства ачивок.
    ///// </summary>
    //public AchievementProperties AchievementProperties;

    #endregion

    #endregion

    #region ICloneable

    public object Clone()
    {
        return new ProjectData()
        {
            InventoryData = (InventoryData)InventoryData.Clone(),
            ProjectProperties = (ProjectProperties)ProjectProperties.Clone(),
            //SelectedPlayerItem = PlayerSelectedData.GetDeepClonedList(SelectedPlayerItem),
            //OpenedItems = OpenedItems.ToList(),
            //DailyGiftData = DailyGiftData.ToList(),
            //AchievementProperties = (AchievementProperties)AchievementProperties.Clone(),
            //RewardForPoints = RewardForPoints.ToList(),
            //OpenMaps = new HashSet<int>(OpenMaps),
            //Purchases = new List<string>(Purchases),
        };
    }

    #endregion

    #region Конструкторы

    public ProjectData()
    { 
        InventoryData = new();
        ProjectProperties = new();
        //AchievementProperties = new();
        //SelectedPlayerItem = new();
        //OpenedItems = new();
        //DailyGiftData = new();
        //RewardForPoints = new();
        //OpenMaps = new() { 0 };
        //Purchases = new();
    }

    #endregion
}

public class PlayerSelectedData : ICloneable
{
    public int PlayerId { get; set; }

    public Dictionary<string, ISelectableItem> SelectedItems = new();

    [JsonConverter(typeof(HashSetConverter<ISelectableItem>))]
    public HashSet<ISelectableItem> SelectedPets = new();

    public object Clone()
    {
        var selectedData = new PlayerSelectedData()
        {
            PlayerId = PlayerId,
            SelectedItems = new Dictionary<string, ISelectableItem>(SelectedItems),
            SelectedPets = SelectedPets.ToHashSet()
        };

        return selectedData;
    }

    public static List<PlayerSelectedData> GetDeepClonedList(List<PlayerSelectedData> list)
    {
        return list
            .Select(item => item.Clone() as PlayerSelectedData)
            .ToList();
    }
}

public class PlayerData
{
    public int PlayerId { get; set; }

    public bool IsMainPlayer { get; set; }
}

