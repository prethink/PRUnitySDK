using System;
using System.Globalization;

/// <summary>
/// Подписи предметов и наград для параметров метрики.
/// </summary>
/// <remarks>
/// Формат один на все события — «название-идентификатор» одной строкой. Название служебное
/// (<c>Name</c>), а не перевод: иначе один предмет разошёлся бы в отчёте по языкам игроков.
/// Идентификатор стоит рядом, потому что названия повторяются и меняются, а он нет.
/// </remarks>
public static class MetricKeys
{
    /// <summary>
    /// Подпись предмета: «название-идентификатор».
    /// </summary>
    public static string Item(ItemDefinitionBase item)
    {
        return item != null ? Join(item.Name, item.Id) : string.Empty;
    }

    /// <summary>
    /// Подпись награды: предмет, ресурс с количеством или контейнер.
    /// </summary>
    /// <remarks>
    /// Количество у ресурса базовое, без множителя за рекламу: множитель события называют сами.
    /// У награды без предмета (действие, контейнер) названием служит имя её ассета.
    /// </remarks>
    public static string Reward(RewardBase reward)
    {
        switch (reward)
        {
            case null:
                return string.Empty;
            case RewardResource resource when resource.Item != null:
                return WithCount(Item(resource.Item), resource.Count);
            case RewardItemBase itemReward when itemReward.Item != null:
                return Item(itemReward.Item);
            case RewardContainerBase container:
                return Join(container.name, container.Id);
            default:
                return reward.name;
        }
    }

    /// <summary>
    /// Дописывает к подписи количество: <c>Coin x500</c>.
    /// </summary>
    public static string WithCount(string key, long count)
    {
        return key + " x" + count.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Склеивает название с идентификатором.
    /// </summary>
    /// <remarks>
    /// У ресурсов они совпадают (<c>Coin</c> и <c>Coin</c>) — тогда остаётся одно название.
    /// </remarks>
    public static string Join(string name, string id)
    {
        if (string.IsNullOrEmpty(name))
            return id ?? string.Empty;

        return string.IsNullOrEmpty(id) || string.Equals(name, id, StringComparison.OrdinalIgnoreCase)
            ? name
            : name + "-" + id;
    }
}
