/// <summary>
/// Видимость постоянного интерфейса.
/// </summary>
/// <remarks>
/// Короткие обёртки над <see cref="HudTracker"/>: состояние и список элементов держит он,
/// а обращаются к интерфейсу обычно отсюда — рядом с окнами, поверх которых он и лежит.
/// </remarks>
public partial class PRWindowsContainer
{
    /// <summary>
    /// Постоянный интерфейс показан.
    /// </summary>
    public bool IsHudVisible => PRUnitySDK.Trackers.Hud.IsVisible;

    /// <summary>
    /// Показывает постоянный интерфейс.
    /// </summary>
    public void ShowHud() => SetHudVisible(true);

    /// <summary>
    /// Прячет постоянный интерфейс: полосы, панель быстрого доступа и подобное.
    /// </summary>
    /// <remarks>
    /// Окна не задевает: их canvas отдельный и лежит выше.
    /// </remarks>
    public void HideHud() => SetHudVisible(false);

    /// <summary>
    /// Задаёт видимость постоянного интерфейса.
    /// </summary>
    /// <param name="isVisible">Показать интерфейс.</param>
    public void SetHudVisible(bool isVisible) => PRUnitySDK.Trackers.Hud.SetVisible(isVisible);
}
