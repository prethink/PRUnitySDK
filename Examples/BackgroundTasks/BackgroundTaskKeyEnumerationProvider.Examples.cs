/// <summary>
/// Ключи задач-примеров.
/// Показывает штатный способ расширения: ключи объявляются `partial`-частью рядом
/// со своим модулем, общий файл SDK править не нужно.
/// </summary>
public partial class BackgroundTaskKeyEnumerations
{
    public static readonly Enumeration PlaytimeTracker = new(nameof(PlaytimeTracker));
    public static readonly Enumeration NewDay = new(nameof(NewDay));
}
