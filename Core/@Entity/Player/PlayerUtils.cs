public static class PlayerUtils
{
    /// <summary>
    /// Имя игрока, у которого своего нет: платформа его не отдала или игрок его скрыл.
    /// </summary>
    /// <remarks>
    /// Провайдер, а не строка: подпись с таким именем переводится вместе с интерфейсом,
    /// в том числе уже висящая на экране. Настоящее имя — данные и не переводится.
    /// </remarks>
    public static readonly ILocalizationProvider DefaultName =
        new LocalizationProvider("player_default_name", "Player", "Игрок", "Oyuncu");

    /// <summary>
    /// Запасное имя на текущем языке. Строка застывает на языке вызова — для подписей
    /// бери <see cref="DefaultName"/>.
    /// </summary>
    public static string GetDefaultName()
    {
        return DefaultName.GetTranslate();
    }
}
