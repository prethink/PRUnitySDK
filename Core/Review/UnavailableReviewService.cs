using System;

/// <summary>
/// Оценка игры без площадки: спросить некого.
/// </summary>
/// <remarks>
/// Служба по умолчанию. Всё, что предлагает игроку оценить игру, при ней молча не появляется:
/// отдельной проверки «есть ли площадка» вызывающему не нужно.
/// </remarks>
public sealed class UnavailableReviewService : IReviewService
{
    /// <inheritdoc />
    public bool CanRequest => false;

    /// <inheritdoc />
    /// <remarks>Не поднимается: доступность не меняется.</remarks>
    public event Action AvailabilityChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    /// <remarks>Не поднимается: окно оценки не открывается.</remarks>
    public event Action<bool> Completed
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool TryRequest() => false;
}
