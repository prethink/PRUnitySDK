/// <summary>
/// Информация о текущем девайсе.
/// </summary>
public abstract class DeviceInfoBase
{
    /// <summary>
    /// Признак, что это компьютер.
    /// </summary>
    /// <returns>True - да, false нет.</returns>
    public abstract bool IsDesktop();

    /// <summary>
    /// Признак, что это смартфон.
    /// </summary>
    /// <returns>True - да, false нет.</returns>
    public abstract bool IsMobile();

    /// <summary>
    /// Признак, что это планшет.
    /// </summary>
    /// <returns>True - да, false нет.</returns>
    public abstract bool IsTablet();

    /// <summary>
    /// Признак, что это телевизор.
    /// </summary>
    /// <returns>True - да, false нет.</returns>
    public abstract bool IsTV();

    /// <summary>
    /// Признак, что это устройство с тач падом.
    /// </summary>
    /// <returns>True - да, false нет.</returns>
    public virtual bool IsTouchDevice()
    {
        return IsMobile() || IsTablet();
    }

    public abstract bool IsIOS();
}
