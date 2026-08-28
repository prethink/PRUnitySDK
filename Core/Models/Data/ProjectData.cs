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
    /// Что выбрано у каждого локального игрока.
    /// </summary>
    /// <remarks>
    /// Список, а не один объект: локальных игроков может быть несколько, и надетое
    /// у них своё, хотя предметы куплены общие.
    /// </remarks>
    public List<PlayerSelectedData> SelectedPlayerItems { get; set; } = new();

    /// <summary>
    /// Идентификаторы открытых предметов.
    /// </summary>
    public Dictionary<string, long> Resources { get; set; } = new();

    /// <summary>
    /// Временные награды: ключ награды и момент окончания её действия.
    /// <para>
    /// Отдельный словарь, а не даты в ProjectProperties: так награды можно перечислить,
    /// проверить на истечение и очистить, не задевая прочие свойства проекта, а ключи
    /// наград не пересекаются с произвольными DateTime-свойствами.
    /// </para>
    /// </summary>
    public Dictionary<string, DateTime> TimeLimitedRewards { get; set; } = new();

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация данных проекта.
    /// </summary>
    public void Initialize()
    {
        ProjectProperties = new();
        OpenedItems = new();
        SelectedPlayerItems = new();
        Resources = new();
        TimeLimitedRewards = new();

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

        // Глубокая копия: у каждого игрока свой словарь выбранного, и общий список
        // ссылок означал бы, что правка в клоне меняет исходные данные.
        clone.SelectedPlayerItems = PlayerSelectedData.GetDeepClonedList(SelectedPlayerItems);
        clone.TimeLimitedRewards = new Dictionary<string, DateTime>(TimeLimitedRewards);

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

/// <summary>
/// Что выбрано у одного локального игрока.
/// </summary>
/// <remarks>
/// Отдельно от владения: <c>OpenedItems</c> отвечает, что у игрока есть, а это —
/// что из этого надето. Данные на игрока, потому что за одной сохранёнкой могут сидеть
/// двое: у каждого своя шапка, хотя куплена она один раз.
/// </remarks>
public class PlayerSelectedData : ICloneable
{
    /// <summary>
    /// Локальный игрок: 0 — первый, 1 — второй.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Выбранный предмет по виду: вид — ключ, идентификатор предмета — значение.
    /// </summary>
    /// <remarks>
    /// Идентификаторами, а не ссылками: в сохранение всё равно попадает только Id,
    /// а предмет может быть убран из состава игры и вернуться обратно.
    /// </remarks>
    public Dictionary<string, string> SelectedItems { get; set; } = new();

    /// <summary>
    /// Выбранные питомцы.
    /// </summary>
    public HashSet<string> SelectedPets { get; set; } = new();

    public object Clone()
    {
        var selectedData = new PlayerSelectedData()
        {
            PlayerId = PlayerId,
            SelectedItems = new Dictionary<string, string>(SelectedItems),
            SelectedPets = new HashSet<string>(SelectedPets)
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

