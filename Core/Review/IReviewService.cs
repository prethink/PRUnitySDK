using System;

/// <summary>
/// Оценка игры на площадке: можно ли попросить и чем закончилась просьба.
/// </summary>
/// <remarks>
/// Окно оценки принадлежит площадке, игра его только открывает. Площадка сама решает, можно
/// ли спросить: игрок не вошёл в аккаунт, уже оценил игру или его в этой сессии уже
/// спрашивали — во всех этих случаях <see cref="CanRequest"/> ложно.
/// </remarks>
public interface IReviewService
{
    /// <summary>
    /// Можно ли сейчас попросить игрока оценить игру.
    /// </summary>
    bool CanRequest { get; }

    /// <summary>
    /// <see cref="CanRequest"/> мог измениться.
    /// </summary>
    event Action AvailabilityChanged;

    /// <summary>
    /// Окно оценки закрыто: <c>true</c> — игрок поставил оценку, <c>false</c> — закрыл без неё.
    /// </summary>
    event Action<bool> Completed;

    /// <summary>
    /// Открывает окно оценки площадки.
    /// </summary>
    /// <returns><c>false</c> — просить нельзя, окно не открыто и <see cref="Completed"/> не придёт.</returns>
    bool TryRequest();
}
