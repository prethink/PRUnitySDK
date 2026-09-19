using System.Collections.Generic;

/// <summary>
/// Подписи, состоящие из одного числа.
/// </summary>
/// <remarks>
/// Число само по себе перевода не требует, а его разряд — требует: <c>12,3K</c>
/// и <c>12,3 тыс.</c> — это одна и та же величина на разных языках. Поэтому даже голое
/// число выводится через источник перевода: наблюдатель пересоберёт его при смене языка.
/// <para>
/// Формат вынесен строкой, а не зашит в код, чтобы язык, которому нужны неразрывный
/// пробел или другой порядок знаков, поправили переводом, а не правкой интерфейса.
/// </para>
/// </remarks>
public static class NumberLabels
{
    /// <summary>
    /// Ключ подписи в базе локализации.
    /// </summary>
    public const string ValueKey = "number_value";

    private static readonly ILocalizationProvider ValueProvider = new LocalizationProvider(
        ValueKey,
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "{0}" },
            { LangType.English, "{0}" },
            { LangType.Turkey, "{0}" }
        });

    /// <summary>
    /// Одно число без сопровождающего текста.
    /// </summary>
    public static ILocalizationProvider Value => ValueProvider;
}
