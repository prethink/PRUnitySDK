using System.Collections.Generic;

/// <summary>
/// Дополняет незаполненные переводы кнопки запасной подписью, сохраняя ключ базы.
/// </summary>
public sealed class UIButtonLabelProvider : ILocalizationProvider
{
    private readonly UIButtonDefinition definition;

    public UIButtonLabelProvider(UIButtonDefinition definition) => this.definition = definition;

    /// <inheritdoc />
    public string LocalizationKey => definition.LocalizationKey;
    /// <inheritdoc />
    public IReadOnlyDictionary<LangType, string> LocalizationValues
    {
        get
        {
            var result = new Dictionary<LangType, string>();
            foreach (var fallback in UIButtonLabels.Action.LocalizationValues)
            {
                string value = null;
                definition.LocalizationValues?.TryGetValue(fallback.Key, out value);
                result[fallback.Key] = string.IsNullOrWhiteSpace(value) ? fallback.Value : value;
            }
            if (PRUnitySDK.IsInitialized && !string.IsNullOrWhiteSpace(LocalizationKey))
            {
                foreach (var translated in L.GetDictionary(LocalizationKey))
                {
                    if (!string.IsNullOrWhiteSpace(translated.Value))
                        result[translated.Key] = translated.Value;
                }
            }
            return result;
        }
    }
}
