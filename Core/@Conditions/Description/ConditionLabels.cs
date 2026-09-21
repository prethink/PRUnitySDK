using System.Collections.Generic;

/// <summary>
/// Подписи условий: что игроку нужно набрать, чтобы условие выполнилось.
/// </summary>
/// <remarks>
/// Подпись пишется форматом целиком, а не склейкой из слова и числа: порядок частей
/// в языках разный, и решать его должен переводчик.
/// <para>
/// У каждой подписи есть ключ, поэтому тела можно заменить на поиск в базе локализации,
/// не трогая места вызова.
/// </para>
/// </remarks>
public static class ConditionLabels
{
    /// <summary>
    /// Подпись требования по числу. Аргумент - само число.
    /// </summary>
    /// <remarks>
    /// Сравнение меняет не только знак, но и смысл фразы: «нужно набрать» и «должно
    /// остаться не больше» - разные требования, и одной подписью со знаком они
    /// не покрываются.
    /// </remarks>
    /// <param name="comparison">Как условие сравнивает накопленное с числом.</param>
    public static ILocalizationProvider Requirement(ConditionComparison comparison)
    {
        return comparison switch
        {
            ConditionComparison.Greater =>
                Provider("condition_requirement_greater", "More than {0} needed", "Нужно больше {0}", "{0}'dan fazla gerekli"),
            ConditionComparison.Equal =>
                Provider("condition_requirement_equal", "Exactly {0} needed", "Нужно ровно {0}", "Tam olarak {0} gerekli"),
            ConditionComparison.NotEqual =>
                Provider("condition_requirement_not_equal", "{0} does not fit", "Столько не подходит: {0}", "{0} uygun değil"),
            ConditionComparison.LessOrEqual =>
                Provider("condition_requirement_less_or_equal", "{0} at most", "Не больше {0}", "En fazla {0}"),
            ConditionComparison.Less =>
                Provider("condition_requirement_less", "Fewer than {0}", "Меньше {0}", "{0}'dan az"),
            _ =>
                Provider("condition_requirement", "{0} needed", "Нужно {0}", "{0} gerekli")
        };
    }

    private static ILocalizationProvider Provider(string key, string english, string russian, string turkish)
    {
        return new LocalizationProvider(key, new Dictionary<LangType, string>
        {
            { LangType.English, english },
            { LangType.Russian, russian },
            { LangType.Turkey, turkish }
        });
    }
}
