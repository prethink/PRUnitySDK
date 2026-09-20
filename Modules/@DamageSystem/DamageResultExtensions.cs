/// <summary>
/// Чтение результата попытки нанести урон.
/// </summary>
public static class DamageResultExtensions
{
    /// <summary>
    /// Попадание принято: результат Damaged или Killed, в том числе при нулевом уроне.
    /// </summary>
    /// <remarks>
    /// Для подробного итога используйте <see cref="DamageOutcome.WasApplied"/>;
    /// фактическую потерю HP показывает <see cref="DamageOutcome.HealthLost"/>.
    /// </remarks>
    /// <param name="result">Результат попытки.</param>
    /// <returns><c>true</c> для <c>Damaged</c> и <c>Killed</c>.</returns>
    public static bool IsApplied(this DamageResult result)
    {
        return result == DamageResult.Damaged || result == DamageResult.Killed;
    }
}
