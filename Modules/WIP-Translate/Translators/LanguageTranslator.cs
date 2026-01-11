using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]  
public abstract class LanguageTranslator : MonoBehaviour, IDropDown
{
    #region Поля и свойства

    /// <summary>
    /// Параметры текста.
    /// </summary>
    private List<string> parameters = new List<string>();

    /// <summary>
    /// Кэш ключей переводов.
    /// </summary>
    private TranslateKeysBase translateCacheKey;

    /// <summary>
    /// Базовый размер шрифта TextMeshPro.
    /// </summary>
    private float baseFontSize;

    /// <summary>
    /// Базовый шрифт для TextMeshPro.
    /// </summary>
    private TMP_FontAsset baseFont;

    /// <summary>
    /// Иницилазиция.
    /// </summary>
    private bool initialized;

    /// <summary>
    /// Перекрытие ключ перевода.
    /// </summary>
    private IOverrideTranslateKey overrideKey;

    /// <summary>
    /// Признак, что ZInject проинициализирован.
    /// </summary>
    private bool zinjectInitialized;

    /// <summary>
    /// Автоматический перевод заблокирован.
    /// </summary>
    private bool blockTranslate;

    /// <summary>
    /// Кастомный переводчик.
    /// </summary>
    private ITranslateble translateble;

    #endregion

    #region MonoBehaviour

    /// <summary>
    /// Ключ перевода.
    /// </summary>
    public abstract string KeyWord { get; protected set; }

    /// <summary>
    /// Элемент TextMeshPro для работы с текстов.
    /// </summary>
    [Tooltip("Элемент TextMeshPro для работы с текстов.")]
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    /// <summary>
    /// Настройки для конкретного языка.
    /// </summary>
    [Tooltip("Настройки для конкретного языка.")]
    [SerializeField] private List<LangSetting> langSettings;

    /// <summary>
    /// Параметры при старте. Используется как заглушка.
    /// </summary>
    [Tooltip("Параметры при старте. Используется как заглушка.")]
    [SerializeField] private List<string> parametersOnStart = new List<string>();

    /// <summary>
    /// Признак, что текст будет обновлен при старте.
    /// </summary>
    [Tooltip("Признак, что текст будет обновлен при старте.")]
    [SerializeField] private bool updateTextOnStart = true;

    /// <summary>
    /// Признак, что требуется инициализация.
    /// </summary>
    [Tooltip("При указание данного признака, работа переводов начнется только после прямого вызова перевода из кода.")]
    [SerializeField] private bool requiredInitialized;


    private void Awake()
    {
        if (textMeshProUGUI == null)
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();

        if (parametersOnStart.Count > 0)
            UpdateParameters(parametersOnStart.ToArray());

        if (overrideKey == null)
            overrideKey = GetComponent<LanguageTranslatorOverrideKeyBase>();
    }

    private void Start()
    {
        if(updateTextOnStart)
            UpdateTranslate();
    }

    private void OnValidate()
    {
        textMeshProUGUI ??= GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        PRUnitySDK.LanguageManager.OnChangeLangEvent += OnChangeLanguage;
        OnChangeLanguage(PRUnitySDK.LanguageManager.GetCurrentLang());
    }

    private void OnDisable()
    {
        PRUnitySDK.LanguageManager.OnChangeLangEvent -= OnChangeLanguage;
    }

    #endregion

    #region Методы

    /// <summary>
    /// Получить ключ перевода.
    /// </summary>
    /// <returns>Ключ перевода.</returns>
    public string GetKey()
    {
        return overrideKey != null 
            ? overrideKey.GetKey() 
            : KeyWord;
    }

    /// <summary>
    /// Установить ключ перевода.
    /// </summary>
    /// <param name="key">Ключ перевода.</param>
    public void SetKey(string key)
    {
        if (overrideKey != null)
            overrideKey.SetKey(key);
        else
            KeyWord = key;
    }

    /// <summary>
    /// Установить перекрытие для получения ключа перевода.
    /// </summary>
    /// <param name="overrideKey">Ключ перекрытия.</param>
    public void SetOverrideKey(IOverrideTranslateKey overrideKey)
    {
        this.overrideKey = overrideKey;
    }

    /// <summary>
    /// Заполнить базовую информацию о шрифтах если не заполенна.
    /// </summary>
    private void WriteDefaultDataIfNull()
    {
        if (baseFontSize == 0)
            baseFontSize = textMeshProUGUI.fontSize;

        if (baseFont == null)
            baseFont = textMeshProUGUI.font;
    }

    /// <summary>
    /// Обновить параметры для текста.
    /// </summary>
    /// <param name="strParams">Параметры для текста.</param>
    public void UpdateParameters(params string[] strParams)
    {
        parameters.Clear();
        parameters.AddRange(strParams);
    }

    /// <summary>
    /// Обновить ключ перевода.
    /// </summary>
    /// <param name="key">Ключ перевода.</param>
    public void UpdateKey(string key)
    {
        translateble = null;
        SetKey(key);
        UpdateTranslate();
    }

    /// <summary>
    /// Обновить параметры.
    /// </summary>
    /// <param name="key">Параметры.</param>
    public void UpdateParams(params string[] strParams)
    {
        UpdateTranslate(strParams);
    }

    /// <summary>
    /// Обновить ключ перевода.
    /// </summary>
    /// <param name="key">Ключ перевода.</param>
    /// <param name="strParams">Параметры для текста.</param>
    public void UpdateKey(string key, params string[] strParams)
    {
        SetKey(key);
        UpdateTranslate(strParams);
    }

    /// <summary>
    /// Заблокировать перевод.
    /// </summary>
    public void BlockTranslate()
    {
        blockTranslate = true;
    }

    /// <summary>
    /// Разблокировать перевод.
    /// </summary>
    public void UnblockTranslate()
    {
        blockTranslate = false;
    }

    /// <summary>
    /// Установить кастомный переводчик.
    /// </summary>
    /// <param name="translatable">Переводчик.</param>
    public void SetTranslatable(ITranslateble translatable)
    {
        initialized = true;
        parameters.Clear();
        this.translateble = translatable;   
    }

    /// <summary>
    /// Установить кастомный переводчик.
    /// </summary>
    /// <param name="translatable">Переводчик.</param>
    /// <param name="strParams">Параметры.</param>
    public void SetTranslatable(ITranslateble translatable, params string[] strParams)
    {
        initialized = true;
        UpdateParameters(strParams);
        this.translateble = translatable;
    }

    /// <summary>
    /// Событие/Метод изменения языка.
    /// </summary>
    /// <param name="lang">Ключ языка.</param>
    protected virtual void OnChangeLanguage(string lang)
    {
        if (requiredInitialized && !initialized || blockTranslate)
            return;

        WriteDefaultDataIfNull();

        if (!zinjectInitialized)
        {
            PRLog.WriteWarning(this, $"ZInject еще не инициализирован! Ключ {GetKey()}. Игровой объект {gameObject.name}");
            return;
        }

        if(translateble != null)
        {
            textMeshProUGUI.SetText(parameters.Count > 0 ? Translator.GetTranslateWithArgs(translateble, parameters.ToArray()) : translateble.GetTranslateByLang(lang));
            return;
        }

        var translateText = PRUnitySDK.LocalizationService.GetTranslation(GetKey(), lang);

        if (!GetKey().IsTranslateKeyEmpty())
        {
            ThrowIfArgsCountMoreParameters(translateText);
            textMeshProUGUI.SetText(string.Format(translateText, parameters.ToArray()));
        }
        UpdateTextProperties(lang);

        var parent = textMeshProUGUI.transform.parent;

        if (gameObject.activeInHierarchy && parent != null && parent.gameObject.activeSelf)
            StartCoroutine(LayoutFixer.FixLayoutCorutina(parent.gameObject));
    }

    /// <summary>
    /// Обновить параметры TextMeshPro с учетом ключа языка.
    /// </summary>
    /// <param name="lang">Ключ языка.</param>
    private void UpdateTextProperties(string lang)
    {
        textMeshProUGUI.fontSize = GetFontSize(lang);
        textMeshProUGUI.font = GetFont(lang);
    }

    /// <summary>
    /// Обновить текст без перевода.
    /// </summary>
    /// <param name="text">Текст.</param>
    public void UpdateText(string text)
    {
        WriteDefaultDataIfNull();
        textMeshProUGUI.SetText(text);
        UpdateTextProperties(PRUnitySDK.LanguageManager.GetCurrentLang());
    }

    /// <summary>
    /// Обновить текущий перевод.
    /// </summary>
    public void UpdateTranslate()
    {
        initialized = true;
        WriteDefaultDataIfNull();
        OnChangeLanguage(PRUnitySDK.LanguageManager.GetCurrentLang());
    }

    public void TryUpdateTranslate()
    {
        if (requiredInitialized && !initialized || blockTranslate)
            return;

        WriteDefaultDataIfNull();
        OnChangeLanguage(PRUnitySDK.LanguageManager.GetCurrentLang());
    }

    /// <summary>
    /// Обновить перевод с параметрами.
    /// </summary>
    /// <param name="strParams">Массив параметров.</param>
    public void UpdateTranslate(params string[] strParams)
    {
        UpdateParameters(strParams);
        UpdateTranslate();
    }

    /// <summary>
    /// Получить текущий размер шрифта по ключу языка.
    /// </summary>
    /// <param name="langKey">Ключ языка.</param>
    /// <returns>Размер шрифта.</returns>
    private float GetFontSize(string langKey)
    {
        var settings = langSettings.FirstOrDefault(x => x.LangKey == langKey);
        if (settings == null || settings.FontSize == 0)
            return baseFontSize;

        return settings.FontSize;
    }

    /// <summary>
    /// Получить текущий шрифт для языка.
    /// </summary>
    /// <param name="langKey">Ключ языка.</param>
    /// <returns>Шрифт TextMeshPro.</returns>
    private TMP_FontAsset GetFont(string langKey)
    {
        return PRUnitySDK.LocalizationService.GetFontOrNull(langKey) ?? baseFont;
    }

    /// <summary>
    /// Выбросить исключение если количество аргументов в тексте больше чем нужно.
    /// </summary>
    /// <param name="text">Текст, который проверяем.</param>
    /// <exception cref="ArgumentException">Исключение.</exception>
    private void ThrowIfArgsCountMoreParameters(string text)
    {
        string pattern = @"\{\d+\}";
        int argsCount = Regex.Matches(text, pattern).Count;
        if (argsCount > parameters.Count)
            throw new ArgumentException($"Количество аргументов в строке ({argsCount}) больше, чем количество параметров ({parameters.Count}). Ключ {GetKey()}. Игровой объект {gameObject.name}");
    }

    #endregion

    #region IDropDown

    public abstract string[] GetKeys();

    #endregion
}

[Serializable]
public class LangSetting
{
    [field: SerializeField]
    public string LangKey { get; private set; }

    [field: SerializeField]
    public float FontSize { get; private set; }
}
