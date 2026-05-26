using TMPro;

public interface ILocalizationService 
{
    public TMP_FontAsset GetFontOrNull(string langKey);
    string GetTranslation(string key, string lang);
}
