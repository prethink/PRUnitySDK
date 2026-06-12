using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationObserver : PRMonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI TextMeshProUGUI;
    [SerializeField] protected string globalKey;
    [SerializeField] protected LocalizationControl localization;
    [SerializeField] protected List<string> localizationArgs = new();

    private ILocalizationProvider localizationProvider;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        localizationProvider ??= localization;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        TextMeshProUGUI ??= GetComponent<TextMeshProUGUI>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PRUnitySDK.LanguageManager.OnChangeLangEvent += OnChangeLanguage;
    }

    private void OnChangeLanguage(string langKey)
    {
        if (TextMeshProUGUI == null)
            return;

        TextMeshProUGUI.text = GetTranslate(langKey);
    }

    private string GetTranslate(string langKey)
    {
        if(!string.IsNullOrEmpty(globalKey))
            return L.Tr(globalKey, localizationArgs.ToArray());

        if (localizationArgs.Any())
            return string.Format(localizationProvider.GetTranslate(langKey), localizationArgs.ToArray());

        return localizationProvider.GetTranslate(langKey); 
    }

    public void SetLocalization(ILocalizationProvider localization, string[] args)
    {
        this.localizationProvider = localization;
        SetArgs(args);
    }

    public void SetLocalization(ILocalizationProvider localization)
    {
        this.SetLocalization(localization, Array.Empty<string>());
    }

    public void SetArgs(string[] args, bool updateText = true)
    {
        localizationArgs.Clear();
        if(args.Length > 0)
            localizationArgs.AddRange(args);

        if (updateText)
            OnChangeLanguage(PRUnitySDK.CurrentLang);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        PRUnitySDK.LanguageManager.OnChangeLangEvent -= OnChangeLanguage;
    }

    protected override void Start()
    {
        base.Start();
        OnChangeLanguage(PRUnitySDK.CurrentLang);
    }


    [Button("RU")]
    void ChangeRU()
    {
        PRUnitySDK.LanguageManager.SwitchLang("ru");
    }

    [Button("EN")]
    void ChangeEN()
    {
        PRUnitySDK.LanguageManager.SwitchLang("en");
    }

    [Button("TR")]
    void ChangeTR()
    {
        PRUnitySDK.LanguageManager.SwitchLang("tr");
    }
}
