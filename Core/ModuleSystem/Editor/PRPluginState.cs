/// <summary>
/// Где сейчас папка стороннего плагина модуля.
/// </summary>
public enum PRPluginState
{
    /// <summary>
    /// Модулю плагин не нужен.
    /// </summary>
    None,

    /// <summary>
    /// Папка на месте, Unity её видит.
    /// </summary>
    Visible,

    /// <summary>
    /// Папка переименована в «Имя~»: Unity её не видит, но файлы на диске.
    /// </summary>
    Hidden,

    /// <summary>
    /// Папки нет ни в каком виде.
    /// </summary>
    Missing
}
