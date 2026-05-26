using UnityEngine;

public class TestLang : MonoBehaviour
{
    public LocalizationRow Localization;

    private void Start()
    {
        Debug.Log(L.Tr("exit"));
        PRUnitySDK.LanguageManager.SwitchLang(LocalizationUtils.GetLanguageCode(LangType.Russian));
        Debug.Log(L.Tr("exit"));

        PRUnitySDK.LanguageManager.SwitchLang(LocalizationUtils.GetLanguageCode(LangType.English));
        Debug.Log(L.Tr("exit"));

        PRUnitySDK.LanguageManager.SwitchLang(LocalizationUtils.GetLanguageCode(LangType.Turkey));
        Debug.Log(L.Tr("exit"));
    }
}
