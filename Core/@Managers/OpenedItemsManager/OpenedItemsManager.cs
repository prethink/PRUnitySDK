using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ведёт открытые игроком предметы: что открыто и сколько этого есть сейчас.
/// </summary>
/// <remarks>
/// Отвечает на два разных вопроса. <see cref="IsOpenedItem(string)"/> — открывался ли
/// предмет когда-либо; ответ не меняется от того, что предмет потратили.
/// <see cref="GetCount(string)"/> — сколько его сейчас.
/// <para>
/// Разделение нужно расходуемым предметам: потратив последний ключ, игрок не перестаёт
/// знать о ключах, а купленный скин не должен снова оказаться в продаже.
/// </para>
/// </remarks>
public class OpenedItemsManager : SingletonProviderBase<OpenedItemsManager>
{
    #region Проверка

    public bool IsOpenedItem(IIdentifiable selectableItem)
    {
        return selectableItem != null && IsOpenedItem(selectableItem.Id);
    }

    /// <summary>
    /// Предмет открывался хотя бы раз.
    /// </summary>
    /// <remarks>
    /// Текущее количество не важно: важен сам факт, что предмет у игрока был.
    /// </remarks>
    public bool IsOpenedItem(string id)
    {
        ItemStack stack = FindStack(id);
        return stack != null && stack.IsOpened;
    }

    public bool IsOpenedItem(Type type, IIdentifiable selectableItem)
    {
        return IsOpenedItem(type.ToString(), selectableItem);
    }

    public bool IsOpenedItem(Type type, string id)
    {
        return IsOpenedItem(type.ToString(), id);
    }

    public bool IsOpenedItem(string type, IIdentifiable selectableItem)
    {
        return selectableItem != null && IsOpenedItem(type, selectableItem.Id);
    }

    /// <summary>
    /// Предмет открыт указанной системой.
    /// </summary>
    public bool IsOpenedItem(string type, string id)
    {
        ItemStack stack = FindStack(id);
        return stack != null && stack.IsOpened && stack.Created == type;
    }

    #endregion

    #region Виды предметов

    /// <summary>
    /// Предмет открыт и относится к указанному виду.
    /// </summary>
    /// <remarks>
    /// Вид — это тип определения: <c>HatDefinition</c>, <c>BrainrotDefinition</c>.
    /// Отдельно от <see cref="IsOpenedItem(string, string)"/>, который спрашивает
    /// про систему-источник: предмет может быть шапкой и при этом прийти из награды.
    /// </remarks>
    public bool IsOpenedInCategory(string category, string id)
    {
        ItemStack stack = FindStack(id);
        return stack != null && stack.IsOpened && stack.HasCategory(category);
    }

    /// <summary>
    /// Предмет открыт и относится к виду <typeparamref name="T"/>.
    /// </summary>
    public bool IsOpenedInCategory<T>(string id) where T : IIdentifiable
    {
        return IsOpenedInCategory(typeof(T).Name, id);
    }

    /// <summary>
    /// Открытые предметы указанного вида.
    /// </summary>
    /// <remarks>
    /// Отвечает на вопрос «что у игрока открыто из шапок» без обхода каталога:
    /// вид записан прямо в сохранении.
    /// </remarks>
    public IEnumerable<ItemStack> GetOpenedByCategory(string category)
    {
        return GetOpenedItems().Where(stack => stack.HasCategory(category));
    }

    /// <summary>
    /// Открытые предметы вида <typeparamref name="T"/>.
    /// </summary>
    public IEnumerable<ItemStack> GetOpenedByCategory<T>() where T : IIdentifiable
    {
        return GetOpenedByCategory(typeof(T).Name);
    }

    /// <summary>
    /// Идентификаторы открытых предметов указанного вида.
    /// </summary>
    public IEnumerable<string> GetOpenedIds(string category)
    {
        return GetOpenedByCategory(category)
            .Where(stack => !string.IsNullOrEmpty(stack.ItemId))
            .Select(stack => stack.ItemId);
    }

    /// <summary>
    /// Виды, из которых у игрока что-то открыто.
    /// </summary>
    public IEnumerable<string> GetOpenedCategories()
    {
        return GetOpenedItems()
            .Select(stack => stack.Category)
            .Where(category => !string.IsNullOrEmpty(category))
            .Distinct();
    }

    /// <summary>
    /// Сколько предметов вида открыто.
    /// </summary>
    public int CountOpenedInCategory(string category)
    {
        return GetOpenedByCategory(category).Count();
    }

    #endregion

    #region Количество

    /// <summary>
    /// Сколько предметов у игрока сейчас.
    /// </summary>
    public int GetCount(IIdentifiable selectableItem)
    {
        return selectableItem == null ? 0 : GetCount(selectableItem.Id);
    }

    /// <summary>
    /// Сколько предметов у игрока сейчас.
    /// </summary>
    public int GetCount(string id)
    {
        return FindStack(id)?.Count ?? 0;
    }

    /// <summary>
    /// Сколько предметов выдано за всё время.
    /// </summary>
    public int GetTotalOpened(string id)
    {
        return FindStack(id)?.TotalOpened ?? 0;
    }

    /// <summary>
    /// У игрока есть нужное количество предметов.
    /// </summary>
    public bool HasCount(IIdentifiable selectableItem, int count)
    {
        return selectableItem != null && GetCount(selectableItem.Id) >= count;
    }

    #endregion

    #region Открытие

    public bool Open(Type type, IIdentifiable selectableItem, bool requiredSave = true)
    {
        return Open(type.ToString(), selectableItem, requiredSave);
    }

    /// <summary>
    /// Отмечает предмет открытым, если он ещё не открыт.
    /// </summary>
    /// <remarks>
    /// Для того, что не считается штуками: брейнрот в коллекции, купленный скин,
    /// открытая способность. Повторный вызов ничего не меняет — количество не растёт,
    /// иначе один и тот же скин превратился бы в «две штуки».
    /// <para>
    /// Возвращаемое значение отвечает на вопрос «это впервые?» — по нему показывают
    /// поздравление или подсвечивают новинку.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/>, если предмет открыт впервые.</returns>
    public bool Open(string type, IIdentifiable selectableItem, bool requiredSave = true)
    {
        if (selectableItem == null || IsOpenedItem(selectableItem.Id))
            return false;

        return Add(type, selectableItem, 1, requiredSave);
    }

    #endregion

    #region Выдача и трата

    public bool Add(Type type, IIdentifiable selectableItem, bool requiredSave = true)
    {
        return Add(type.ToString(), selectableItem, requiredSave);
    }

    /// <summary>
    /// Добавляет указанное количество предметов от имени заданного типа системы.
    /// </summary>
    /// <remarks>
    /// Для расходуемого: ключей, билетов, патронов. Тому, что просто «есть или нет»,
    /// нужен <see cref="Open(string, IIdentifiable, bool)"/>.
    /// </remarks>
    public bool Add(Type type, IIdentifiable selectableItem, int count, bool requiredSave = true)
    {
        return Add(type.ToString(), selectableItem, count, requiredSave);
    }

    public bool Add(string type, IIdentifiable selectableItem, bool requiredSave = true)
    {
        return Add(type, selectableItem, 1, requiredSave);
    }

    public bool Add(string type, IIdentifiable selectableItem, int count, bool requiredSave = true)
    {
        if (selectableItem == null || count <= 0)
            return false;

        List<ItemStack> items = GetItems();

        if (items == null)
            return false;

        ItemStack stack = items.FirstOrDefault(x => x.HasItem(selectableItem.Id));

        if (stack == null)
        {
            stack = ItemStack.CreateEmpty(type, selectableItem);
            items.Add(stack);
        }
        else if (string.IsNullOrEmpty(stack.Category))
        {
            // Запись из сохранения, сделанного до появления вида: восстанавливаем его,
            // пока тип предмета известен - в данных остался только идентификатор.
            stack.Category = ItemStack.ResolveCategory(selectableItem);
        }

        stack.Add(count);

        if (requiredSave)
            GameManager.Instance.SaveProjectData();

        return true;
    }

    /// <summary>
    /// Тратит предметы, оставляя запись об открытии.
    /// </summary>
    /// <remarks>
    /// Запись не удаляется даже при нулевом остатке: она хранит факт открытия, и удаление
    /// вернуло бы предмет в продажу или заново заблокировало бы его в интерфейсе.
    /// </remarks>
    /// <returns><see langword="false"/>, если предметов меньше, чем нужно.</returns>
    public bool TryRemoveItem(IIdentifiable selectableItem, int count = 1, bool requiredSave = true)
    {
        if (selectableItem == null)
            return false;

        ItemStack stack = FindStack(selectableItem.Id);

        if (stack == null || !stack.TryRemove(count))
            return false;

        if (requiredSave)
            GameManager.Instance.SaveProjectData();

        return true;
    }

    #endregion

    #region Доступ к данным

    /// <summary>
    /// Все открытые предметы.
    /// </summary>
    public IEnumerable<ItemStack> GetOpenedItems()
    {
        return GetItems()?.Where(stack => stack != null && stack.IsOpened)
               ?? Enumerable.Empty<ItemStack>();
    }

    private static ItemStack FindStack(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return GetItems()?.FirstOrDefault(stack => stack != null && stack.HasItem(id));
    }

    private static List<ItemStack> GetItems()
    {
        return GameManager.Instance.GetProjectData()?.OpenedItems;
    }

    #endregion
}
