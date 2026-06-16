public static class NumberConverter
{
    /// <summary>
    /// Форматирует большое число в строку с суффиксами (k, M, B и т.д.).
    /// </summary>
    /// <param name="value">Число для форматирования.</param>
    /// <returns>Отформатированная строка.</returns>
    public static string FormatNumber(long value, int minDigitsBeforeShorten = 5)
    {
        string[] suffixes = { "", "K", "M", "B", "T", "P", "E" }; // Суффиксы
        int suffixIndex = 0;

        // Если число короче, чем заданное количество символов, просто возвращаем его
        if (value.ToString().Length < minDigitsBeforeShorten)
            return value.ToString();

        double formattedValue = value;

        // Уменьшаем число, пока оно не станет меньше 1000, и увеличиваем индекс суффикса.
        while (formattedValue >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            formattedValue /= 1000.0;
            suffixIndex++;
        }

        // Форматируем с одним знаком после запятой, если число дробное.
        return formattedValue % 1 == 0
            ? $"{(long)formattedValue}{suffixes[suffixIndex]}"
            : $"{formattedValue:F1}{suffixes[suffixIndex]}";
    }
}
