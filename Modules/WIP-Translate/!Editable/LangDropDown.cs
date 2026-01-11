public class LangDropDown : IDropDown
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
