/// <summary>
/// Нажата экранная кнопка, описанная <see cref="UIButtonDefinition"/>.
/// </summary>
/// <remarks>
/// Приходит на каждое нажатие — мышью, касанием или горячей клавишей — после того, как
/// кнопка отработала. Какая именно кнопка, слушатель узнаёт сравнением с нужным ассетом:
/// описание кнопки одно на все панели, где она стоит.
/// <para>
/// Нужен тем, кто следит за действиями игрока со стороны: шагу обучения «открой магазин»,
/// подсказке, счётчику. Сама кнопка о них не знает.
/// </para>
/// </remarks>
public interface IUIButtonPressedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Обрабатывает нажатие кнопки.
    /// </summary>
    /// <param name="button">Описание нажатой кнопки.</param>
    /// <param name="executed">
    /// Кнопка сработала: действие выполнено и окно открыто. <c>false</c> — нажатие было,
    /// но действие отказало или окно не открылось.
    /// </param>
    void OnUIButtonPressed(UIButtonDefinition button, bool executed);
}

/// <summary>
/// Рассылка событий экранных кнопок.
/// </summary>
public static class UIButtonEvents
{
    /// <summary>
    /// Сообщает подписчикам шины о нажатии кнопки.
    /// </summary>
    /// <param name="button">Описание нажатой кнопки.</param>
    /// <param name="executed">Кнопка сработала.</param>
    public static void RaisePressed(UIButtonDefinition button, bool executed)
    {
        EventBus.RaiseEvent<IUIButtonPressedEvent>(listener => listener.OnUIButtonPressed(button, executed));
    }
}
