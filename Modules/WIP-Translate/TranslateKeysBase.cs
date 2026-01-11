public abstract class TranslateKeysBase : IDropDown
{
    /// <summary>
    /// Пустое значение перевода.
    /// </summary>
    public const string EMPTY = "-";

    #region IDropDown

    public abstract string[] GetKeys();

    #endregion
}
