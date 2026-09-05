/// <summary>
/// Открытый игроком предмет: что это, сколько его сейчас и открывался ли он вообще.
/// </summary>
/// <remarks>
/// <see cref="Category"/> отвечает, что за предмет, <see cref="Count"/> — сколько его
/// сейчас, <see cref="IsOpened"/> — открывал ли игрок его когда-либо. Последний назад
/// не отыгрывается: потратив последний ключ, игрок не перестаёт знать о ключах.
/// <para>
/// Хранится идентификатор, а не ссылка на определение: в сохранение всё равно попадает
/// только он. Определение берут из каталога, когда оно нужно.
/// </para>
/// </remarks>
public class ItemStack
{
    /// <summary>
    /// Система, открывшая предмет.
    /// </summary>
    /// <remarks>
    /// Отвечает на вопрос «откуда он взялся»: покупка, награда, лутбокс.
    /// </remarks>
    public string Created { get; set; }

    /// <summary>
    /// Вид предмета: шапка, брейнрот, ключ.
    /// </summary>
    /// <remarks>
    /// Хранится в данных, а не выводится из типа: после загрузки тип уже неизвестен.
    /// </remarks>
    public string Category { get; set; }

    /// <summary>
    /// Идентификатор открытого предмета.
    /// </summary>
    public string ItemId { get; set; }

    /// <summary>
    /// Сколько предметов у игрока сейчас.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Сколько предметов выдано за всё время.
    /// </summary>
    /// <remarks>
    /// Отдельно от <see cref="Count"/>, потому что трата уменьшает только текущее
    /// количество. По этому числу и определяется факт открытия.
    /// </remarks>
    public int TotalOpened { get; set; }

    /// <summary>
    /// Предмет открывался хотя бы раз.
    /// </summary>
    /// <remarks>
    /// <see cref="Count"/> учитывается ради сохранений, сделанных до появления
    /// <see cref="TotalOpened"/>: там счётчика всего полученного нет, и единственный
    /// след открытия — ненулевое количество.
    /// </remarks>
    public bool IsOpened => TotalOpened > 0 || Count > 0;

    /// <summary>
    /// Запись относится к указанному предмету.
    /// </summary>
    /// <remarks>
    /// Количество не проверяется: запись существует — значит предмет открывали.
    /// </remarks>
    public bool HasItem(string id)
    {
        return !string.IsNullOrEmpty(ItemId) && ItemId == id;
    }

    /// <summary>
    /// Запись относится к указанному виду предметов.
    /// </summary>
    public bool HasCategory(string category)
    {
        return string.Equals(Category, category, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Выдаёт предметы.
    /// </summary>
    public void Add(int count)
    {
        if (count <= 0)
            return;

        Count += count;
        TotalOpened += count;
    }

    /// <summary>
    /// Тратит предметы, если их хватает.
    /// </summary>
    /// <remarks>
    /// Запись остаётся даже при нулевом количестве: она хранит факт открытия,
    /// а он не отменяется тратой.
    /// </remarks>
    /// <returns><see langword="false"/>, если предметов меньше, чем нужно.</returns>
    public bool TryRemove(int count)
    {
        if (count <= 0 || Count < count)
            return false;

        Count -= count;
        return true;
    }

    public static ItemStack Create(string created, IIdentifiable item, int count = 1)
    {
        return new ItemStack()
        {
            Created = created,
            Category = ResolveCategory(item),
            ItemId = item?.Id,
            Count = count,
            TotalOpened = count
        };
    }

    public static ItemStack CreateEmpty(string type, IIdentifiable item, int count = 1)
    {
        return Create(type, item, 0);
    }

    /// <summary>
    /// Определяет вид предмета по его типу.
    /// </summary>
    /// <remarks>
    /// Единственный момент, когда тип ещё известен: дальше предмет живёт в сохранении
    /// как идентификатор и вид.
    /// </remarks>
    public static string ResolveCategory(IIdentifiable item)
    {
        return item?.GetType().Name ?? string.Empty;
    }
}
