/// <summary>
/// Известные источники, откуда предмет достаётся не покупкой.
/// </summary>
/// <remarks>
/// Строки, а не перечисление: список открыт. Модуль, которого в SDK ещё нет, назовёт себя
/// сам, и реестр примет его без правок здесь. Известные названия собраны в одном месте,
/// чтобы витрина могла их перевести.
/// </remarks>
public static class ReservedItemSources
{
    /// <summary>
    /// Выдаётся за достижение.
    /// </summary>
    public const string Achievement = "Achievement";

    /// <summary>
    /// Попадается в ежедневном подарке.
    /// </summary>
    public const string DailyGift = "DailyGift";

    /// <summary>
    /// Попадается в бесплатном подарке по таймеру.
    /// </summary>
    public const string FreeGift = "FreeGift";

    /// <summary>
    /// Выпадает из кейса или колеса удачи.
    /// </summary>
    public const string Case = "Case";

    /// <summary>
    /// Лежит в ящике на уровне.
    /// </summary>
    public const string LootContainer = "LootContainer";
}
