using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ведёт выбранное каждым локальным игроком: что из имеющегося надето.
/// </summary>
/// <remarks>
/// Отвечает на вопрос, отличный от <see cref="OpenedItemsManager"/>. Тот знает, что у
/// игрока есть, а этот — что из этого сейчас на нём. Разница видна на разделённом экране:
/// шапка куплена одна, а надета она может быть только у первого игрока.
/// <para>
/// Вид предмета служит ключом: на игроке одновременно одна шапка, один скин, один цвет.
/// Выбор нового вытесняет прежний сам, без снятия вручную.
/// </para>
/// </remarks>
public class SelectedItemsManager : SingletonProviderBase<SelectedItemsManager>
{
    /// <summary>
    /// Возвращает выбранный предмет указанного вида.
    /// </summary>
    /// <param name="playerId">Локальный игрок: 0 — первый, 1 — второй.</param>
    /// <param name="category">Вид предмета.</param>
    /// <returns>Идентификатор предмета либо пустая строка.</returns>
    public string GetSelectedId(int playerId, string category)
    {
        PlayerSelectedData data = FindData(playerId);

        return data != null && !string.IsNullOrEmpty(category)
               && data.SelectedItems.TryGetValue(category, out string itemId)
            ? itemId
            : string.Empty;
    }

    /// <summary>
    /// Предмет выбран этим игроком.
    /// </summary>
    public bool IsSelected(int playerId, ItemDefinitionBase item)
    {
        return item != null && GetSelectedId(playerId, GetCategory(item)) == item.Id;
    }

    /// <summary>
    /// Выбирает предмет для игрока.
    /// </summary>
    /// <remarks>
    /// Вид берётся из типа определения, поэтому шапка вытесняет шапку, но не скин.
    /// Владение здесь не проверяется: выдать предмет решает вызывающий — надеть можно
    /// и то, что досталось наградой, а не покупкой.
    /// </remarks>
    /// <returns><see langword="true"/>, если выбор изменился.</returns>
    /// <param name="ignoreSaveCooldown">Записать на диск не дожидаясь кулдауна.</param>
    public bool Select(
        int playerId,
        ItemDefinitionBase item,
        bool requiredSave = true,
        bool ignoreSaveCooldown = false)
    {
        if (item == null)
            return false;

        string category = GetCategory(item);

        if (string.IsNullOrEmpty(category))
            return false;

        PlayerSelectedData data = GetOrCreateData(playerId);

        if (data == null)
            return false;

        if (data.SelectedItems.TryGetValue(category, out string current) && current == item.Id)
            return false;

        data.SelectedItems[category] = item.Id;

        if (requiredSave)
            GameManager.Instance.SaveProjectData(ignoreSaveCooldown);

        return true;
    }

    /// <summary>
    /// Снимает выбор указанного вида.
    /// </summary>
    /// <returns><see langword="true"/>, если что-то было выбрано.</returns>
    public bool Clear(
        int playerId,
        string category,
        bool requiredSave = true,
        bool ignoreSaveCooldown = false)
    {
        PlayerSelectedData data = FindData(playerId);

        if (data == null || string.IsNullOrEmpty(category) || !data.SelectedItems.Remove(category))
            return false;

        if (requiredSave)
            GameManager.Instance.SaveProjectData(ignoreSaveCooldown);

        return true;
    }

    /// <summary>
    /// Всё выбранное игроком: вид — идентификатор предмета.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetSelection(int playerId)
    {
        PlayerSelectedData data = FindData(playerId);
        return data != null ? data.SelectedItems : new Dictionary<string, string>();
    }

    /// <summary>
    /// Вид предмета — ключ выбора.
    /// </summary>
    /// <remarks>
    /// Имя типа определения: <c>HatDefinition</c>, <c>ObbySkinDefinition</c>. Так же
    /// помечаются записи в <see cref="OpenedItemsManager"/>, и два хранилища говорят
    /// об одном и том же одинаково.
    /// </remarks>
    public static string GetCategory(ItemDefinitionBase item)
    {
        return item != null ? item.GetType().Name : string.Empty;
    }

    private static PlayerSelectedData FindData(int playerId)
    {
        List<PlayerSelectedData> items = GetItems();

        return items?.FirstOrDefault(data => data != null && data.PlayerId == playerId);
    }

    private static PlayerSelectedData GetOrCreateData(int playerId)
    {
        List<PlayerSelectedData> items = GetItems();

        if (items == null)
            return null;

        PlayerSelectedData data = items.FirstOrDefault(x => x != null && x.PlayerId == playerId);

        if (data != null)
            return data;

        data = new PlayerSelectedData { PlayerId = playerId };
        items.Add(data);

        return data;
    }

    private static List<PlayerSelectedData> GetItems()
    {
        ProjectData projectData = GameManager.Instance.GetProjectData();

        if (projectData == null)
            return null;

        return projectData.SelectedPlayerItems ??= new List<PlayerSelectedData>();
    }
}
