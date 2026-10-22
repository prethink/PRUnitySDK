#if !PRSDK_DISABLE_GAMEPUSH
using System;
using GamePush;

/// <summary>
/// Оценка игры через GamePush (<c>GP_App.ReviewRequest</c>).
/// </summary>
/// <remarks>
/// Можно ли спросить, решает площадка (<c>GP_App.CanReview</c>): например, игрок уже оценил игру
/// или площадка оценок не поддерживает. Как и на Яндексе, за сессию спрашиваем один раз.
/// <para>
/// Ответ приходит одним из двух вызовов: оценка поставлена — с числом звёзд, окно закрыто или
/// ошибка — с текстом ошибки.
/// </para>
/// </remarks>
public sealed class GamePushReviewService : IReviewService
{
    private bool requested;

    /// <inheritdoc />
    public event Action AvailabilityChanged;

    /// <inheritdoc />
    public event Action<bool> Completed;

    /// <inheritdoc />
    public bool CanRequest => !requested && GP_Init.isReady && GP_App.CanReview();

    /// <inheritdoc />
    public bool TryRequest()
    {
        if (!CanRequest)
            return false;

        requested = true;
        GP_App.ReviewRequest(_ => Finish(true), _ => Finish(false));

        AvailabilityChanged?.Invoke();
        return true;
    }

    private void Finish(bool sent)
    {
        Completed?.Invoke(sent);
        AvailabilityChanged?.Invoke();
    }
}
#endif
