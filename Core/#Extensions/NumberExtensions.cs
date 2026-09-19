using System;

/// <summary>
/// Приведение чисел с защитой от переполнения.
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Приводит значение к <see cref="long"/>, обрезая его по нулю снизу
    /// и по <see cref="long.MaxValue"/> сверху.
    /// </summary>
    public static long ClampToLong(this decimal value) => value.ClampToLong(0L, long.MaxValue);

    /// <summary>
    /// Приводит значение к <see cref="long"/> в заданных границах.
    /// Дробная часть отбрасывается.
    /// </summary>
    public static long ClampToLong(this decimal value, long min, long max) => (long)Math.Clamp(value, min, max);

    /// <summary>
    /// Складывает значения, упирая результат в границы <see cref="decimal"/>.
    /// </summary>
    /// <remarks>
    /// Диапазон decimal огромен, но конечен, и на переполнении он не «заворачивается»,
    /// а бросает исключение. Там, где число накапливается без предела — счёт опыта,
    /// ресурсов, — из-за одного начисления упала бы вся игра, поэтому такой счёт
    /// останавливается на максимуме.
    /// </remarks>
    public static decimal AddSafe(this decimal value, decimal addValue)
    {
        try
        {
            return value + addValue;
        }
        catch (OverflowException)
        {
            return addValue > 0m ? decimal.MaxValue : decimal.MinValue;
        }
    }

    /// <summary>
    /// Умножает значения, упирая результат в границы <see cref="decimal"/>.
    /// </summary>
    /// <remarks>
    /// Переполнение здесь ближе, чем при сложении: множитель применяют к уже большому
    /// числу. Поведение то же, что у <see cref="AddSafe"/> — упереться в предел,
    /// а не упасть.
    /// </remarks>
    public static decimal MultiplySafe(this decimal value, decimal multiplier)
    {
        try
        {
            return value * multiplier;
        }
        catch (OverflowException)
        {
            bool isPositive = (value > 0m) == (multiplier > 0m);
            return isPositive ? decimal.MaxValue : decimal.MinValue;
        }
    }

    /// <summary>
    /// Отбрасывает отрицательное значение.
    /// </summary>
    public static decimal ClampToPositive(this decimal value) => value < 0m ? 0m : value;
}
