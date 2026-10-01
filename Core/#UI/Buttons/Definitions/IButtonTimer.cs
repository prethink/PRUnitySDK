using System;

/// <summary>
/// Источник обратного отсчёта экранной кнопки: сколько осталось ждать, пока станет
/// доступно то, что открывает кнопка.
/// </summary>
/// <remarks>
/// Реализации живут рядом со своими системами (подарки, колесо удачи) и выбираются в
/// <see cref="UIButtonDefinition"/> так же, как условие внимания. Состояния не меняют -
/// только смотрят: их опрашивает <see cref="UIButtonTimerWatcher"/> раз в секунду.
/// </remarks>
public interface IButtonTimer
{
    /// <summary>
    /// Сколько осталось ждать.
    /// </summary>
    /// <param name="remaining">Оставшееся время; имеет смысл, только если метод вернул <see langword="true"/>.</param>
    /// <returns><see langword="false"/>, если ждать нечего - таймер на кнопке скрыт.</returns>
    bool TryGetRemaining(out TimeSpan remaining);
}
