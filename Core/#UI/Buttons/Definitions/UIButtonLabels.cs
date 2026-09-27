using System.Collections.Generic;

/// <summary>
/// Запасные подписи экранных кнопок.
/// </summary>
public static class UIButtonLabels
{
    public static readonly ILocalizationProvider Action = new LocalizationProvider("ui_button_action",
        new Dictionary<LangType, string>
        {
            { LangType.English, "Action" },
            { LangType.Russian, "Действие" },
            { LangType.Turkey, "Eylem" }
        });

    public static readonly ILocalizationProvider Attention = new LocalizationProvider("ui_button_attention",
        new Dictionary<LangType, string>
        {
            { LangType.English, "!" },
            { LangType.Russian, "!" },
            { LangType.Turkey, "!" }
        });
}
