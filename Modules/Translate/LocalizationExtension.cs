using SABI;
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
    /// осталось бы пустым, и текст молча перестал бы переводиться — в редакторе при
    /// этом всё работает. Интерфейс, который строится в рантайме, попадал под это целиком.
    /// </remarks>
    public static LocalizationObserver GetLanguageComponent(this TextMeshProUGUI textMesh)
    {
        if (textMesh == null)
            return null;

        LocalizationObserver languageComponent = textMesh.GetComponent<LocalizationObserver>();

        if (languageComponent == null)
            languageComponent = textMesh.AddComponent<LocalizationObserver>();

        if (languageComponent.TextMeshProUGUI == null)
            languageComponent.TextMeshProUGUI = textMesh;

        return languageComponent;
    }
}
