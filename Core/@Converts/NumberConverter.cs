using System;

public static class NumberConverter
{
    /// <summary>
    /// Латинские суффиксы разрядов. До E хватало целому счёту, дальше — разряды, до которых
    /// дорастает только дробный: decimal вмещает почти на десять порядков больше long.
    /// </summary>
    /// <remarks>
    /// Запасной вариант: пока подписи разрядов не переведены или база локализации ещё
    /// не поднялась, число выводится этими.
    /// </remarks>
    private static readonly string[] FallbackSuffixes = { "", "K", "M", "B", "T", "P", "E", "Z", "Y", "R" };

    /// <summary>
    /// Откуда берутся подписи разрядов.
    /// </summary>
    /// <remarks>
    /// Ставит модуль локализации при инициализации. Пустой поставщик — не ошибка:
    /// в редакторе и в тестах числа печатаются латиницей.
    /// </remarks>
    public static INumberSuffixProvider SuffixProvider { get; set; }

    /// <summary>
    /// Сколько разрядов умеет подписывать конвертер.
    /// </summary>
    public static int SuffixCount => FallbackSuffixes.Length;

    /// <summary>
    /// Латинская подпись разряда: она же запасная, если перевода нет.
    /// </summary>
    /// <param name="power">Номер разряда: 0 — без сокращения, 1 — тысячи.</param>
    public static string GetFallbackSuffix(int power)
    {
        if (power <= 0 || power >= FallbackSuffixes.Length)
            return string.Empty;

        return FallbackSuffixes[power];
    }

    /// <summary>
    /// Форматирует большое число в строку с суффиксами (k, M, B и т.д.).
    /// </summary>
    /// <param name="value">Число для форматирования.</param>
    /// <returns>Отформатированная строка.</returns>
    public static string FormatNumber(long value, int minDigitsBeforeShorten = 5)
    {
        return FormatNumber((decimal)value, minDigitsBeforeShorten);
    }

    /// <summary>
    /// Форматирует дробное число в строку с суффиксами.
    /// </summary>
    /// <remarks>
    /// Дробная часть на разряд не влияет: сокращать или нет, решает целая, а хвост
    /// у короткого числа округляется до двух знаков — «12,3456789» на экране читается хуже,
    /// чем «12,35».
    /// <para>
    /// Подписи разрядов переводятся, поэтому строка зависит от текущего языка: её нельзя
    /// посчитать один раз и запомнить. Там, где число попадает в подпись аргументом, его
    /// отдают функцией — тогда наблюдатель пересоберёт его при смене языка.
    /// </para>
    /// </remarks>
    /// <param name="value">Число для форматирования.</param>
    /// <param name="minDigitsBeforeShorten">
    /// Сколько знаков должно быть в целой части, чтобы число сокращалось суффиксом.
    /// </param>
    /// <returns>Отформатированная строка.</returns>
    public static string FormatNumber(decimal value, int minDigitsBeforeShorten = 5)
    {
        int suffixIndex = 0;

        // Если число короче, чем заданное количество символов, просто возвращаем его
        if (decimal.Truncate(value).ToString().Length < minDigitsBeforeShorten)
            return Math.Round(value, 2).ToString("0.##");

        decimal formattedValue = value;

        // Уменьшаем число, пока оно не станет меньше 1000, и увеличиваем индекс суффикса.
        while (Math.Abs(formattedValue) >= 1000m && suffixIndex < FallbackSuffixes.Length - 1)
        {
            formattedValue /= 1000m;
            suffixIndex++;
        }

        string suffix = GetSuffix(suffixIndex);

        // Форматируем с одним знаком после запятой, если число дробное.
        return formattedValue % 1m == 0m
            ? $"{decimal.Truncate(formattedValue)}{suffix}"
            : $"{formattedValue:F1}{suffix}";
    }

    /// <summary>
    /// Подпись разряда на текущем языке.
    /// </summary>
    /// <remarks>
    /// Поставщик может ответить пустым — например, перевода для этого разряда ещё нет.
    /// Тогда берётся латинская подпись: число без разряда читалось бы меньше себя в тысячу
    /// раз, и это опаснее непереведённой буквы.
    /// </remarks>
    private static string GetSuffix(int power)
    {
        string translated = SuffixProvider?.GetSuffix(power);

        return string.IsNullOrEmpty(translated) ? GetFallbackSuffix(power) : translated;
    }
}
