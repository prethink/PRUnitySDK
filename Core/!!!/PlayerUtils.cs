using System.Collections.Generic;

public static class PlayerUtils 
{
    public static string GetDefaultName()
    {
        Dictionary<LangType, string> defaultPlayerNameDict = new Dictionary<LangType, string>()
        {
            { LangType.Russian, "Игрок" },
            { LangType.English, "Player" },
            { LangType.Turkey, "Oyuncu" }
        };
        var defaultPlayerName = new Translator("Default player name", defaultPlayerNameDict);

        return defaultPlayerName.GetTranslateByLang(PRUnitySDK.CurrentLang);
    }
}
