/// <summary>
/// Подписи экрана загрузки, которые видит игрок.
/// </summary>
public static class LoadingScreenLabels
{
    /// <summary>
    /// Заголовок экрана.
    /// </summary>
    public static readonly ILocalizationProvider Title = new LocalizationProvider(
        "loading_title", "Loading...", "Загрузка...", "Yükleniyor...");

    /// <summary>
    /// Пояснение под заголовком: что происходит и сколько ждать.
    /// </summary>
    /// <remarks>
    /// Про действие игры, а не про её размер: «игра большая» звучит как оправдание и ничего не
    /// обещает, а «несколько секунд» говорит игроку, что ждать недолго.
    /// </remarks>
    public static readonly ILocalizationProvider Description = new LocalizationProvider(
        "loading_description",
        "Getting the game ready. This may take a few seconds.",
        "Подготавливаем игру. Это может занять несколько секунд.",
        "Oyun hazırlanıyor. Bu birkaç saniye sürebilir.");
}
