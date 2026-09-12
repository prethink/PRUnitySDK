/// <summary>
/// Точка публикации показа и скрытия постоянного интерфейса.
/// </summary>
/// <remarks>
/// Постоянный интерфейс — полосы здоровья, панель быстрого доступа, полоса опыта — живёт
/// не только на общем canvas: полосы над сущностями висят в мире, каждая на своём. Поэтому
/// прятать его одним <c>canvas.enabled</c> нельзя, и о смене состояния сообщает событие.
/// <para>
/// Само состояние держит <see cref="PRWindowsContainer"/>: опоздавший интерфейс спрашивает
/// его при создании, а не ждёт следующего события.
/// </para>
/// </remarks>
public static class HudVisibilityEvents
{
    /// <summary>
    /// Публикует смену видимости постоянного интерфейса.
    /// </summary>
    /// <param name="isVisible">Интерфейс показан.</param>
    public static void RaiseVisibilityChanged(bool isVisible) =>
        EventBus.RaiseEvent<IHudVisibilityEvent>(subscriber =>
            subscriber.OnHudVisibilityChanged(new HudVisibilityEventArgs(isVisible)));
}

/// <summary>
/// Контекст смены видимости постоянного интерфейса.
/// </summary>
public sealed class HudVisibilityEventArgs : EventArgsBase
{
    /// <summary>
    /// Интерфейс показан.
    /// </summary>
    public bool IsVisible { get; }

    /// <summary>
    /// Создаёт контекст смены видимости.
    /// </summary>
    /// <param name="isVisible">Интерфейс показан.</param>
    public HudVisibilityEventArgs(bool isVisible)
    {
        IsVisible = isVisible;
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Hud");
    }
}

/// <summary>
/// Подписчик на показ и скрытие постоянного интерфейса.
/// </summary>
public interface IHudVisibilityEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает смену видимости постоянного интерфейса.
    /// </summary>
    /// <param name="args">Контекст смены видимости.</param>
    void OnHudVisibilityChanged(HudVisibilityEventArgs args);
}
