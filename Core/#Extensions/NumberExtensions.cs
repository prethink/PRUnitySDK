/// <summary>
/// Приведение чисел с защитой от переполнения.
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Приводит значение к <see cref="long"/>, обрезая его по нулю снизу
    /// и по <see cref="long.MaxValue"/> сверху.
    /// </summary>
    /// <param name="value">Исходное значение.</param>
    public static long ClampToLong(this decimal value)
    {
        if (value <= 0m)
            return 0L;

        return value >= long.MaxValue ? long.MaxValue : (long)value;
    }
}
