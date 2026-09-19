using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Держит текст переведённым: подписывается на смену языка и перерисовывает его сам.
/// </summary>
/// <remarks>
/// Вешается на <see cref="TMPro.TextMeshProUGUI"/> руками на префабе или кодом через
/// <see cref="LocalizationExtension.SetLocalization(TextMeshProUGUI, ILocalizationProvider)"/>.
/// Без него строка остаётся на языке, который был в момент отрисовки.
/// <para>
/// Источник текста берётся первый заданный: ключ в базе локализации
/// (<see cref="globalKey"/>) или провайдер, который носит перевод с собой. Аргументы
/// подставляются через <c>string.Format</c>, поэтому в переводе пишут <c>{0}</c>,
/// а не склеивают строку в коде.
/// </para>
/// <para>
/// Когда перевод нужен только по ключу из базы, хватит <see cref="LocalizationKeyObserver"/>:
/// у него нет ни провайдера, ни своего словаря на объекте.
/// </para>
/// </remarks>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationObserver : LocalizationObserverBase
{
    /// <summary>
    /// Ключ в базе локализации. Если задан, перевод берётся по нему.
    /// </summary>
    [SerializeField] protected string globalKey;

    /// <summary>
    /// Перевод, заданный прямо на объекте.
    /// </summary>
    [SerializeField] protected LocalizationControl localization;

    private ILocalizationProvider localizationProvider;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        localizationProvider ??= localization;
    }

    /// <summary>
    /// Задаёт источник перевода и аргументы к нему.
    /// </summary>
    public void SetLocalization(ILocalizationProvider localization, string[] args)
    {
        this.localizationProvider = localization;
        SetArgs(args);
    }

    /// <summary>
    /// Задаёт источник перевода без аргументов.
    /// </summary>
    public void SetLocalization(ILocalizationProvider localization)
    {
        this.SetLocalization(localization, Array.Empty<string>());
    }

    /// <summary>
    /// Задаёт источник перевода и аргументы, которые пересобираются при смене языка.
    /// </summary>
    /// <remarks>
    /// Так отдают аргументы, зависящие от языка: число с сокращённым разрядом
    /// (<c>12,3K</c>) на другом языке пишется другой буквой, и готовая строка осталась бы
    /// от прежнего.
    /// </remarks>
    public void SetLocalization(ILocalizationProvider localization, Func<string[]> args)
    {
        this.localizationProvider = localization;
        SetArgs(args);
    }

    /// <summary>
    /// Задаёт ключ в базе локализации.
    /// </summary>
    /// <remarks>
    /// Ключ сильнее провайдера: пока он задан, перевод берётся по нему. Чтобы вернуться
    /// к провайдеру, ключ очищают пустой строкой.
    /// </remarks>
    public void SetGlobalKey(string key)
    {
        this.globalKey = key;
        Refresh();
    }

    /// <summary>
    /// Задаёт ключ вместе с аргументами.
    /// </summary>
    public void SetGlobalKey(string key, string[] args)
    {
        this.globalKey = key;
        SetArgs(args);
    }

    /// <inheritdoc />
    protected override bool TryGetTranslate(string langKey, out string result)
    {
        result = string.Empty;

        if (!string.IsNullOrEmpty(globalKey))
        {
            result = Format(L.Tr(globalKey), globalKey);
            return true;
        }

        if (localizationProvider == null)
            return false;

        result = Format(localizationProvider.GetTranslate(langKey), localizationProvider.LocalizationKey);
        return true;
    }
}
