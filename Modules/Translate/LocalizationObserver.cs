using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Держит текст переведённым: подписывается на смену языка и перерисовывает его сам.
/// </summary>
/// <remarks>
/// <para>
/// Вешается на <see cref="TMPro.TextMeshProUGUI"/> — руками на префабе или кодом через
/// <see cref="LocalizationExtension.SetLocalization(TextMeshProUGUI, ILocalizationProvider)"/>.
/// Именно он делает перевод живым: без него строка застывает на том языке, который был
/// в момент отрисовки, и меняется только при пересборке окна.
/// </para>
/// <para>
/// Источник текста может быть двух видов, и выбирается первый заданный:
/// ключ в базе локализации (<see cref="globalKey"/>) или провайдер, который носит перевод
/// с собой, — например сам ассет предмета. Аргументы подставляются в текст через
/// <c>string.Format</c>, поэтому в переводе пишут <c>{0}</c>, а не склеивают строку в коде.
/// </para>
/// </remarks>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationObserver : PRMonoBehaviour
{
    /// <summary>
    /// Текст, которым управляет наблюдатель.
    /// </summary>
    [field: SerializeField] public TextMeshProUGUI TextMeshProUGUI;

    /// <summary>
    /// Ключ в базе локализации. Если задан, перевод берётся по нему.
    /// </summary>
    [SerializeField] protected string globalKey;

    /// <summary>
    /// Перевод, заданный прямо на объекте.
    /// </summary>
    [SerializeField] protected LocalizationControl localization;

    /// <summary>
    /// Аргументы для подстановки в перевод.
    /// </summary>
    [SerializeField] protected List<string> localizationArgs = new();

    private ILocalizationProvider localizationProvider;

    /// <summary>
    /// Подписка состоялась: язык на момент включения был доступен.
    /// </summary>
    private string[] argsCache = Array.Empty<string>();

    /// <summary>
    /// Подписка на смену языка уже сделана.
    /// </summary>
    /// <remarks>
    /// Страховка от повторного входа: объект могут выключить и включить снова, а сигнал
    /// готовности к этому моменту уже отработал. Без флага обработчик попал бы в список
    /// дважды, и каждый такой цикл добавлял бы к нему ещё один.
    /// </remarks>
    private bool isSubscribed;

    /// <summary>
    /// Готовности SDK уже дождались или ждём.
    /// </summary>
    private bool isReadyRequested;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        localizationProvider ??= localization;

        // Список аргументов сериализуется и может прийти с префаба, а кеш — нет.
        RebuildArgsCache();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        TextMeshProUGUI ??= GetComponent<TextMeshProUGUI>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Ждём готовности SDK, а не пробуем подписаться на удачу: объект на сцене
        // может включиться раньше, чем поднимется SDK, и менеджера языка тогда ещё нет.
        // Сигнал вызывает и опоздавшего, и того, кто пришёл уже после готовности,
        // поэтому одной точки входа хватает на оба случая.
        if (!isReadyRequested)
        {
            isReadyRequested = true;
            PRUnitySDK.ReadySignal.SubscribeOnReady(OnSDKReady);

            return;
        }

        // Повторное включение: сигнал уже отработал, а язык мог смениться,
        // пока объект был выключен, — поэтому не только подписка, но и перерисовка.
        OnSDKReady();
    }

    private void OnSDKReady()
    {
        if (this == null || !isActiveAndEnabled)
            return;

        Subscribe();
        Refresh();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isSubscribed && PRUnitySDK.LanguageManager != null)
            PRUnitySDK.LanguageManager.OnChangeLangEvent -= OnChangeLanguage;

        isSubscribed = false;
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

    /// <summary>
    /// Меняет аргументы подстановки.
    /// </summary>
    /// <param name="args">Аргументы; <c>null</c> означает «без аргументов».</param>
    /// <param name="updateText">Перерисовать текст сразу.</param>
    public void SetArgs(string[] args, bool updateText = true)
    {
        localizationArgs.Clear();

        if (args != null && args.Length > 0)
            localizationArgs.AddRange(args);

        // Массив держим готовым: перевод пересобирают и при смене подставляемого числа,
        // а не только языка, и каждый раз копировать список незачем.
        RebuildArgsCache();

        if (updateText)
            Refresh();
    }

    /// <summary>
    /// Перерисовывает текст на текущем языке.
    /// </summary>
    /// <remarks>
    /// Нужен, когда перевод остался прежним, а изменилось что-то вокруг: например,
    /// подставляемое число. Для смены языка вызывать не нужно — это делает подписка.
    /// </remarks>
    public void Refresh()
    {
        if (PRUnitySDK.LanguageManager == null)
            return;

        OnChangeLanguage(PRUnitySDK.CurrentLang);
    }

    private void RebuildArgsCache()
    {
        argsCache = localizationArgs.Count > 0 ? localizationArgs.ToArray() : Array.Empty<string>();
    }

    private void Subscribe()
    {
        if (isSubscribed || PRUnitySDK.LanguageManager == null)
            return;

        PRUnitySDK.LanguageManager.OnChangeLangEvent += OnChangeLanguage;
        isSubscribed = true;
    }

    private void OnChangeLanguage(string langKey)
    {
        if (TextMeshProUGUI == null)
            return;

        if (!TryGetTranslate(langKey, out string translated))
            return;

        TextMeshProUGUI.SetText(translated);
        TextMeshProUGUI.ForceMeshUpdate();
    }

    /// <summary>
    /// Собирает текст на нужном языке.
    /// </summary>
    /// <remarks>
    /// Возвращает <c>false</c>, когда источника перевода нет вовсе: наблюдатель повесили,
    /// но ни ключа, ни провайдера не задали. Тогда текст не трогаем — иначе стёрли бы
    /// то, что написал дизайнер прямо в компоненте.
    /// </remarks>
    private bool TryGetTranslate(string langKey, out string result)
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

    /// <summary>
    /// Подставляет аргументы в перевод.
    /// </summary>
    /// <remarks>
    /// Перевод — данные, а не код: фигурная скобка в тексте или лишний <c>{1}</c>
    /// роняют <c>string.Format</c>. Падать из-за одной строки нельзя, поэтому показываем
    /// перевод как есть и пишем в лог, где именно ошиблись.
    /// </remarks>
    private string Format(string translated, string key)
    {
        if (localizationArgs.Count == 0 || string.IsNullOrEmpty(translated))
            return translated;

        try
        {
            return string.Format(translated, argsCache);
        }
        catch (FormatException)
        {
            PRLog.WriteWarning(this,
                $"Перевод \"{key}\" не принял {localizationArgs.Count} аргументов: \"{translated}\". Показан без подстановки.");

            return translated;
        }
    }
}
