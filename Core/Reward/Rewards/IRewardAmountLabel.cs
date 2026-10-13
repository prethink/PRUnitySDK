using System;

/// <summary>
/// Короткая подпись количества награды для тесных мест — сектора колеса, карточки подарка: «$12K», «+340», «×2 10м».
/// </summary>
/// <remarks>
/// Ресурсная награда (<see cref="RewardResource"/>) показывает своё число и так. Остальные награды
/// без этой подписи показывали бы в секторе только иконку или полное название, которое туда не влезает.
/// </remarks>
public interface IRewardAmountLabel
{
    /// <summary>
    /// Подпись; <c>null</c> — короткой подписи у награды нет.
    /// </summary>
    ILocalizationProvider AmountLabel { get; }

    /// <summary>
    /// Подстановки в подпись; зовутся при каждом показе и смене языка. <c>null</c> — без подстановок.
    /// </summary>
    Func<string[]> AmountArgs { get; }
}
