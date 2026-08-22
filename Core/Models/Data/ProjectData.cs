using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public partial class ProjectData : ICloneable
{
    #region Поля и свойства

    /// <summary>
    /// Свойства проекта.
    /// </summary>
    public ProjectProperties ProjectProperties { get; set; } = new();

    /// <summary>
    /// Идентификаторы открытых предметов.
    /// </summary>
    public List<ItemStack> OpenedItems { get; set; } = new();

    /// <summary>
    /// Идентификаторы открытых предметов.
    /// </summary>
    public Dictionary<string, long> Resources { get; set; } = new();

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация данных проекта.
    /// </summary>
    public void Initialize()
    {
        ProjectProperties = new();
        OpenedItems = new();
        Resources = new();

        this.RunMethodHooks(MethodHookStage.Initializing);
    }

    #endregion

    #region ICloneable

    /// <summary>
    /// Создаёт копию данных проекта. Целевой объект передаётся в хуки стадии
    /// <see cref="MethodHookStage.Cloning"/> аргументом, а не через поле экземпляра -
    /// иначе два одновременных клонирования одного объекта перезаписывали бы друг другу
    /// промежуточное состояние, и модули писали бы свои данные в чужой клон.
    /// </summary>
    public object Clone()
    {
        var clone = new ProjectData();

        clone.ProjectProperties = (ProjectProperties)ProjectProperties.Clone();
        clone.Resources = new Dictionary<string, long>(Resources);
        clone.OpenedItems = OpenedItems.ToList();

        this.RunMethodHooks(MethodHookStage.Cloning, clone);

        return clone;
    }

    #endregion

    #region Конструкторы

    public ProjectData()
    {
        Initialize();
    }

    #endregion
}

public class PlayerSelectedData : ICloneable
{
    public int PlayerId { get; set; }

    public Dictionary<string, ISelectableItem> SelectedItems { get; set; } = new();

    [JsonConverter(typeof(HashSetConverter<ISelectableItem>))]
    public HashSet<ISelectableItem> SelectedPets { get; set; } = new();

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

