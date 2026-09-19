using System;
using TMPro;

/// <summary>
/// Короткие способы получить перевод и повесить его на текст.
/// </summary>
public static class LocalizationExtension
{
    /// <summary>
    /// Перевод на текущем языке.
    /// </summary>
    public static string GetTranslate(this ILocalizationProvider localization)
    {
        return PRLocalization.GetTranslate(localization);
    }

    /// <summary>
    /// Перевод на языке с указанным ключом.
    /// </summary>
    public static string GetTranslate(this ILocalizationProvider localization, string langKey)
    {
        return PRLocalization.GetTranslate(localization, langKey);
    }

    /// <summary>
    /// Перевод на указанном языке.
    /// </summary>
    public static string GetTranslate(this ILocalizationProvider localization, LangType lang)
    {
        return PRLocalization.GetTranslate(localization, lang);
    }

    /// <summary>
    /// Привязывает к тексту источник перевода и аргументы к нему.
    /// </summary>
    /// <remarks>
    /// Это основной способ выводить текст игроку. В отличие от присваивания в
    /// <c>text</c>, подпись остаётся живой: <see cref="LocalizationObserver"/> сам
    /// перерисует её при смене языка, в том числе в уже открытом окне.
    /// </remarks>
    public static void SetLocalization(this TextMeshProUGUI textMesh, ILocalizationProvider localization, string[] args)
    {
        LocalizationObserver languageComponent = textMesh.GetLanguageComponent();

        if (languageComponent != null)
            languageComponent.SetLocalization(localization, args);
    }

    /// <summary>
    /// Привязывает к тексту источник перевода.
    /// </summary>
    public static void SetLocalization(this TextMeshProUGUI textMesh, ILocalizationProvider localization)
    {
        LocalizationObserver languageComponent = textMesh.GetLanguageComponent();

        if (languageComponent != null)
            languageComponent.SetLocalization(localization);
    }

    /// <summary>
    /// Привязывает к тексту источник перевода и аргументы, которые пересобираются
    /// при смене языка.
    /// </summary>
    /// <remarks>
    /// Так отдают аргументы, зависящие от языка. Прежде всего это числа: у сокращённого
    /// числа подпись разряда переводится (<c>12,3K</c> против <c>12,3 тыс.</c>), и готовая
    /// строка после смены языка осталась бы от прежнего.
    /// </remarks>
    public static void SetLocalization(this TextMeshProUGUI textMesh, ILocalizationProvider localization,
        Func<string[]> args)
    {
        LocalizationObserver languageComponent = textMesh.GetLanguageComponent();

        if (languageComponent != null)
            languageComponent.SetLocalization(localization, args);
    }

    /// <summary>
    /// Показывает в тексте одно число и держит его переведённым.
    /// </summary>
    /// <remarks>
    /// Для подписей, которые состоят из числа и ничего больше: счётчик в ячейке хотбара,
    /// количество в списке. Присвоенное в <c>text</c>, такое число выглядит непереводимым,
    /// но его разряд — тоже слово, и при смене языка он должен меняться.
    /// <para>
    /// Число берётся функцией: наблюдатель зовёт её на каждую смену языка. Менять само
    /// значение по ходу игры она не обязана — достаточно, чтобы отдавала текущее.
    /// </para>
    /// </remarks>
    /// <param name="textMesh">Текст, в котором показывается число.</param>
    /// <param name="value">Источник числа.</param>
    /// <param name="minDigitsBeforeShorten">
    /// С какой длины целой части число сокращается разрядом.
    /// </param>
    public static void SetLocalizedNumber(this TextMeshProUGUI textMesh, Func<decimal> value,
        int minDigitsBeforeShorten = 5)
    {
        if (value == null)
            return;

        textMesh.SetLocalization(NumberLabels.Value,
            () => new[] { NumberConverter.FormatNumber(value.Invoke(), minDigitsBeforeShorten) });
    }

    /// <summary>
    /// Показывает в тексте одно число, не меняющееся до следующего вызова.
    /// </summary>
    public static void SetLocalizedNumber(this TextMeshProUGUI textMesh, decimal value,
        int minDigitsBeforeShorten = 5)
    {
        textMesh.SetLocalizedNumber(() => value, minDigitsBeforeShorten);
    }

    /// <summary>
    /// Привязывает к тексту ключ из базы локализации.
    /// </summary>
    /// <remarks>
    /// Тот же живой перевод, но текст берётся по ключу — так подключают строки,
    /// которые правит дизайнер, не трогая код.
    /// </remarks>
    public static void SetLocalizationKey(this TextMeshProUGUI textMesh, string key, params string[] args)
    {
        LocalizationObserver languageComponent = textMesh.GetLanguageComponent();

        if (languageComponent != null)
            languageComponent.SetGlobalKey(key, args);
    }

    /// <summary>
    /// Наблюдатель за языком на этом тексте: если его нет, он добавляется.
    /// </summary>
    /// <remarks>
    /// Ссылку на текст проставляем здесь. Сам наблюдатель берёт её в <c>OnValidate</c>,
    /// а тот в собранной игре не вызывается: у компонента, добавленного кодом, поле
    /// осталось бы пустым, и текст перестал бы переводиться, хотя в редакторе всё
    /// работает. Интерфейс, который строится в рантайме, попадал под это целиком.
    /// </remarks>
    public static LocalizationObserver GetLanguageComponent(this TextMeshProUGUI textMesh)
    {
        if (textMesh == null)
            return null;

        LocalizationObserver languageComponent = textMesh.gameObject.GetOrAddComponent<LocalizationObserver>();

        if (languageComponent.TextMeshProUGUI == null)
            languageComponent.TextMeshProUGUI = textMesh;

        return languageComponent;
    }
}
