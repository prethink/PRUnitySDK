/// <summary>
/// Коды языков, которыми оперирует внешний слой: сохранения, платформа, переводы.
/// </summary>
/// <remarks>
/// Прежнее имя LangDropDown осталось от интерфейса выпадающих списков, которого
/// больше нет: список языков давно строится из LangType.
/// </remarks>
public class LangCode
{
    /// <summary>
    /// Русский.
    /// </summary>
    public const string RU = "ru";

    /// <summary>
    /// Английский.
    /// </summary>
    public const string EN = "en";

    /// <summary>
    /// Турецкий
    /// </summary>
    public const string TR = "tr";

    public string[] GetKeys()
    {
        return new string[] { RU, EN, TR };
    }
}
