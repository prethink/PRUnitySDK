using TMPro;
using UnityEngine.UI;

public static class TranslateExtension
{
    public static void UpdateTranslateKey(this TextMeshProUGUI textMesh, string key)
    {
        textMesh.GetLanguageComponent().UpdateKey(key);
    }

    public static void BlockTranslate(this TextMeshProUGUI textMesh)
    {
        textMesh.GetLanguageComponent().BlockTranslate();
    }

    public static void SetTranslatable(this TextMeshProUGUI textMesh, ITranslateble translateble)
    {
        textMesh.GetLanguageComponent().SetTranslatable(translateble);
        textMesh.UpdateTranslate();
    }

    public static void SetTranslatable(this TextMeshProUGUI textMesh, ITranslateble translateble, params string[] strParams)
    {
        textMesh.GetLanguageComponent().SetTranslatable(translateble, strParams);
        textMesh.UpdateTranslate();
    }

    public static void UpdateTranslateKey(this TextMeshProUGUI textMesh, string key, params string[] strParams)
    {
        textMesh.GetLanguageComponent().UpdateKey(key, strParams);
    }

    public static void UpdateTranslateParams(this TextMeshProUGUI textMesh, params string[] strParams)
    {
        var translatable = textMesh.GetComponent<ITranslateble>();
        if (translatable != null)
            textMesh.SetTranslatable(translatable);
        textMesh.GetLanguageComponent().UpdateParams(strParams);
    }

    public static void UpdateTranslate(this TextMeshProUGUI textMesh, bool refreshLayout = true)
    {
        textMesh.GetLanguageComponent().UpdateTranslate();
        if(refreshLayout)
            textMesh.transform.parent.gameObject.RefreshLayoutGroupsImmediateAndRecursive();  
    }

    public static void TryUpdateTranslate(this TextMeshProUGUI textMesh, bool refreshLayout = true)
    {
        var langComponent = textMesh.TryGetLanguageComponent();
        if (langComponent == null)
            return;

        textMesh.GetLanguageComponent().UpdateTranslate();
        if (refreshLayout)
            textMesh.transform.parent.gameObject.RefreshLayoutGroupsImmediateAndRecursive();
    }

    public static void UpdateTranslate(this TextMeshProUGUI textMesh, params string[] strParams)
    {
        textMesh.GetLanguageComponent().UpdateTranslate(strParams);
    }

    public static void UpdateText(this TextMeshProUGUI textMesh, string text)
    {
        textMesh.BlockTranslate();
        textMesh.GetLanguageComponent(false).UpdateText(text);
    }

    public static void UpdateTranslateKey(this Button button, string key)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().UpdateTranslateKey(key);
    }

    public static void SetTranslatable(this Button button, ITranslateble translateble)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().SetTranslatable(translateble);
    }

    public static void UpdateTranslateKey(this Button button, string key, params string[] strParams)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().UpdateTranslateKey(key, strParams);
    }

    public static void UpdateTranslate(this Button button)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().UpdateTranslate();
    }

    public static void TryUpdateTranslate(this Button button)
    {
        var textButton = button.GetComponentInChildren<TextMeshProUGUI>();
        textButton?.TryUpdateTranslate();
    }

    public static void UpdateTranslate(this Button button, params string[] strParams)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().UpdateTranslate(strParams);
    }

    public static void UpdateText(this Button button, string text)
    {
        button.GetComponentInChildren<TextMeshProUGUI>().UpdateText(text);
    }

    public static bool IsTranslateKeyEmpty(this string key)
    {
        return key.Equals(TranslateKeysBase.EMPTY);
    }

    public static LanguageTranslator GetLanguageComponent(this TextMeshProUGUI textMesh, bool unblockTranslate = true)
    {
        var languageComponent = textMesh.GetComponent<LanguageTranslator>();
        if (languageComponent == null)
            PRLog.WriteError(nameof(TranslateExtension), $"{textMesh.name} не найден компонент {nameof(LanguageTranslator)}");

        if(unblockTranslate)
            languageComponent.UnblockTranslate();
        return languageComponent;
    }

    public static LanguageTranslator TryGetLanguageComponent(this TextMeshProUGUI textMesh, bool unblockTranslate = true)
    {
        var languageComponent = textMesh.GetComponent<LanguageTranslator>();
        if (languageComponent == null)
            return null;

        if (unblockTranslate)
            languageComponent.UnblockTranslate();
        return languageComponent;
    }


}
