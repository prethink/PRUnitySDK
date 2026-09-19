using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Общая механика живого перевода: подписка на смену языка, аргументы подстановки
/// и перерисовка текста.
/// </summary>
/// <remarks>
/// Наследник отвечает ровно за одно — откуда берётся строка. Остальное одинаково
/// у всех: дождаться готовности SDK, подписаться на смену языка, подставить
/// аргументы и не упасть на плохом формате.
/// <para>
/// Наблюдателей на текст должно быть не больше одного: каждый пишет в тот же
/// <see cref="TMPro.TextMeshProUGUI"/>, и на экран попадает тот, кто отработал последним.
/// </para>
/// </remarks>
public abstract class LocalizationObserverBase : PRMonoBehaviour
{
    /// <summary>
    /// Текст, которым управляет наблюдатель.
    /// </summary>
    [field: SerializeField] public TextMeshProUGUI TextMeshProUGUI;

    /// <summary>
    /// Аргументы для подстановки в перевод.
    /// </summary>
    [SerializeField] protected List<string> localizationArgs = new();

    /// <summary>
    /// Аргументы, готовые к передаче в <c>string.Format</c>.
    /// </summary>
    private string[] argsCache = Array.Empty<string>();

    /// <summary>
    /// Источник аргументов, если их пересобирают на каждый показ.
    /// </summary>
    /// <remarks>
    /// Готовая строка в аргументе застывает на языке, который был в момент вызова: перевод
    /// подписи наблюдатель перерисует, а подставленное в неё число — нет. Это заметно там,
    /// где число само переводится: у сокращённых разрядов (<c>12,3K</c>) подпись разряда
    /// на разных языках разная.
    /// </remarks>
    private Func<string[]> argsProvider;

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

        WarnOnDuplicateObserver();
        Subscribe();
        Refresh();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Снимаем подписку с сигнала: объект могли уничтожить до готовности SDK, и тогда
    /// делегат держал бы уничтоженный компонент до конца сессии.
    /// </remarks>
    protected override void UnRegisterEventsOnDestroy()
    {
        if (isReadyRequested && PRUnitySDK.ReadySignal != null)
            PRUnitySDK.ReadySignal.UnSubscribe(OnSDKReady);

        base.UnRegisterEventsOnDestroy();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isSubscribed && PRUnitySDK.LanguageManager != null)
            PRUnitySDK.LanguageManager.OnChangeLangEvent -= OnChangeLanguage;

        isSubscribed = false;
    }

    /// <summary>
    /// Меняет аргументы подстановки.
    /// </summary>
    /// <param name="args">Аргументы; <c>null</c> означает «без аргументов».</param>
    /// <param name="updateText">Перерисовать текст сразу.</param>
    public void SetArgs(string[] args, bool updateText = true)
    {
        argsProvider = null;
        ApplyArgs(args);

        if (updateText)
            Refresh();
    }

    /// <summary>
    /// Задаёт аргументы функцией: они пересобираются при каждой перерисовке.
    /// </summary>
    /// <remarks>
    /// Нужен там, где сам аргумент зависит от языка — прежде всего у чисел с сокращёнными
    /// разрядами. С готовой строкой подпись после смены языка переведена, а число в ней
    /// осталось на прежнем.
    /// <para>
    /// Функцию зовут на каждую смену языка и на каждый <see cref="Refresh"/>, поэтому она
    /// должна быть дешёвой и не менять ничего вокруг.
    /// </para>
    /// </remarks>
    /// <param name="provider">Источник аргументов; <c>null</c> означает «без аргументов».</param>
    /// <param name="updateText">Перерисовать текст сразу.</param>
    public void SetArgs(Func<string[]> provider, bool updateText = true)
    {
        argsProvider = provider;
        ApplyArgs(provider?.Invoke());

        if (updateText)
            Refresh();
    }

    private void ApplyArgs(string[] args)
    {
        localizationArgs.Clear();

        if (args != null && args.Length > 0)
            localizationArgs.AddRange(args);

        // Массив держим готовым: перевод пересобирают и при смене подставляемого числа,
        // а не только языка, и каждый раз копировать список незачем.
        RebuildArgsCache();
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

    /// <summary>
    /// Собирает текст на нужном языке.
    /// </summary>
    /// <remarks>
    /// Возвращает <c>false</c>, когда источника перевода нет вовсе: наблюдателя повесили,
    /// но что переводить, не задали. Тогда текст не трогаем — иначе стёрли бы то,
    /// что написал дизайнер прямо в компоненте.
    /// </remarks>
    protected abstract bool TryGetTranslate(string langKey, out string result);

    /// <summary>
    /// Подставляет аргументы в перевод.
    /// </summary>
    /// <remarks>
    /// Перевод — данные, а не код: фигурная скобка в тексте или лишний <c>{1}</c>
    /// роняют <c>string.Format</c>. Падать из-за одной строки нельзя, поэтому показываем
    /// перевод как есть и пишем в лог, где именно ошиблись.
    /// </remarks>
    protected string Format(string translated, string key)
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

        // Аргументы пересобираем до перевода: у числа с сокращённым разрядом подпись
        // разряда своя на каждом языке, и старая строка осталась бы в новой подписи.
        if (argsProvider != null)
            ApplyArgs(argsProvider.Invoke());

        if (!TryGetTranslate(langKey, out string translated))
            return;

        TextMeshProUGUI.SetText(translated);
        TextMeshProUGUI.ForceMeshUpdate();
    }

    /// <summary>
    /// Предупреждает о втором наблюдателе на том же объекте.
    /// </summary>
    /// <remarks>
    /// Разновидностей наблюдателя стало несколько, и повесить рядом две — вопрос одного
    /// клика или одного <c>SetLocalization</c> на префабе, где наблюдатель уже стоит.
    /// Ошибки при этом нет, просто подпись показывает то, чего от неё не ждут, — поэтому
    /// говорим об этом вслух, а не гасим один из компонентов за разработчика.
    /// </remarks>
    private void WarnOnDuplicateObserver()
    {
        var observers = GetComponents<LocalizationObserverBase>();

        if (observers.Length < 2)
            return;

        PRLog.WriteWarning(this,
            $"На объекте \"{name}\" {observers.Length} наблюдателей перевода — текст перепишет тот, кто отработает последним. Оставьте один.");
    }
}
