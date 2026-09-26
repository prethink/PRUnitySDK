/// <summary>
/// Откуда окно берёт переход.
/// </summary>
public enum MonoWindowTransitionMode
{
    /// <summary>
    /// Переход из настроек проекта (<c>PRSDKSettings → Window Transition</c>).
    /// </summary>
    Default = 0,

    /// <summary>
    /// Свой переход окна (<c>Transition Override</c>): другой пресет или свои значения.
    /// </summary>
    Override = 1,

    /// <summary>
    /// Без перехода: окно появляется и закрывается сразу.
    /// </summary>
    None = 2,
}
