using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationObserver : PRMonoBehaviour
{
    protected TextMeshProUGUI textMeshProUGUI;
    [SerializeField] protected LocalizationRow localization;
    [SerializeField] protected List<string> localizationArgs = new();

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        textMeshProUGUI ??= GetComponent<TextMeshProUGUI>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PRUnitySDK.LanguageManager.OnChangeLangEvent += OnChangeLanguage;
    }

    private void OnChangeLanguage(string langKey)
    {
        textMeshProUGUI.text = localizationArgs.Any() 
            ? string.Format(localization.GetTranslate(langKey), localizationArgs.ToArray())
            : localization.GetTranslate(langKey);
    }

    public void SetArgs(string[] args, bool updateText = true)
    {
        localizationArgs.Clear();
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
