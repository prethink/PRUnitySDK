/// <summary>
/// Подписи разрядов числа из базы локализации.
/// </summary>
/// <remarks>
/// Ключи вида <c>number_suffix_k</c>, <c>number_suffix_m</c> и далее по латинской
/// раскладке разрядов. Ключа в базе нет — остаётся латинская буква, поэтому подключать
/// провайдер можно до того, как переводы заведены: до этого момента числа выглядят как
/// раньше.
/// <para>
/// Перевод нужен не везде одинаково: в английском <c>B</c> — billion, а в турецком
/// <c>bin</c> — тысяча, и на одном и том же числе игроки прочли бы разницу в миллион раз.
/// </para>
/// </remarks>
public sealed class LocalizationNumberSuffixProvider : INumberSuffixProvider
{
    /// <summary>
    /// Общее начало ключей разрядов.
    /// </summary>
    public const string KeyPrefix = "number_suffix_";

    /// <inheritdoc />
    public string GetSuffix(int power)
    {
        if (power <= 0)
            return string.Empty;

        string fallback = NumberConverter.GetFallbackSuffix(power);

        if (string.IsNullOrEmpty(fallback))
            return string.Empty;

        return L.TryTr(GetKey(power), out string translated) ? translated : fallback;
    }

    /// <summary>
    /// Ключ разряда в базе локализации.
    /// </summary>
    /// <param name="power">Номер разряда: 1 — тысячи, 2 — миллионы.</param>
    public static string GetKey(int power)
    {
        return $"{KeyPrefix}{NumberConverter.GetFallbackSuffix(power).ToLowerInvariant()}";
    }
}
