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
}
