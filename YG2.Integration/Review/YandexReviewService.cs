#if !PRSDK_DISABLE_YG2
#if Review_yg
using System;
using YG;

/// <summary>
/// Оценка игры на Яндекс Играх через модуль YG2 <c>Review</c>.
/// </summary>
/// <remarks>
/// Можно ли спросить, площадка сообщает один раз, при запуске (<c>YG2.reviewCanShow</c>):
/// игрок вошёл в аккаунт и ещё не оценивал игру. Спрашивать разрешено раз за сессию,
/// поэтому после запроса служба сама считает оценку недоступной, не дожидаясь ответа.
/// </remarks>
public sealed class YandexReviewService : IReviewService
{
    private bool requested;

    /// <inheritdoc />
    public event Action AvailabilityChanged;

    /// <inheritdoc />
    public event Action<bool> Completed;

    /// <inheritdoc />
    public bool CanRequest => !requested && YG2.isSDKEnabled && YG2.reviewCanShow;

    /// <inheritdoc />
    public bool TryRequest()
    {
        if (!CanRequest)
            return false;

        requested = true;

        // Подписка перед самым запросом, а не в конструкторе: в редакторе плагин при своём
        // запуске обнуляет событие, и подписчик, пришедший раньше, остался бы без ответа.
        YG2.onReviewSent -= HandleReviewSent;
        YG2.onReviewSent += HandleReviewSent;

        YG2.ReviewShow();

        AvailabilityChanged?.Invoke();
        return true;
    }

    private void HandleReviewSent(bool sent)
    {
        YG2.onReviewSent -= HandleReviewSent;

        Completed?.Invoke(sent);
        AvailabilityChanged?.Invoke();
    }
}
#endif
#endif
