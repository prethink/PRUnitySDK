public partial class PRUnitySDK
{
    /// <summary>
    /// Признак, того что сейчас открыто окно.
    /// </summary>
    public static bool IsWindowOpen { get; private set; }

    /// <summary>
    /// Установить признак, что окно открыто или нет. 
    /// </summary>
    /// <param name="isOpen">True - открыто, False - закрыто.</param>
    public static void SetWindowsState(bool isOpen)
    {
        IsWindowOpen = isOpen;
    }
}
