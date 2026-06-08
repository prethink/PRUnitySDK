using System.Collections.Generic;

public class QualityLocalizationProvider : ILocalizationProvider
{
    private readonly QualityType quality;

    public QualityLocalizationProvider(QualityType quality)
    {
        this.quality = quality;
    }

    public string LocalizationKey => $"quality_{quality.ToString().ToLower()}";

    public IReadOnlyDictionary<LangType, string> LocalizationValues => Get(quality);

    public static IReadOnlyDictionary<LangType, string> Get(QualityType type)
    {
        return type switch
        {
            QualityType.Common => Common,
            QualityType.Uncommon => Uncommon,
            QualityType.Rare => Rare,
            QualityType.Mythic => Mythic,
            QualityType.Epic => Epic,
            QualityType.Legendary => Legendary,
            QualityType.Ancient => Ancient,
            QualityType.Godlike => Godlike,
            _ => Unknown
        };
    }

    private static readonly IReadOnlyDictionary<LangType, string> Common =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Обычный" },
            { LangType.English, "Common" },
            { LangType.Turkey, "Sıradan" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Uncommon =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Необычный" },
            { LangType.English, "Uncommon" },
            { LangType.Turkey, "Sıradışı" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Rare =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Редкий" },
            { LangType.English, "Rare" },
            { LangType.Turkey, "Nadir" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Mythic =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Мифический" },
            { LangType.English, "Mythic" },
            { LangType.Turkey, "Efsanevi" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Epic =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Эпический" },
            { LangType.English, "Epic" },
            { LangType.Turkey, "Epik" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Legendary =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Легендарный" },
            { LangType.English, "Legendary" },
            { LangType.Turkey, "Efsanevi" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Ancient =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Древний" },
            { LangType.English, "Ancient" },
            { LangType.Turkey, "Kadim" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Godlike =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Божественный" },
            { LangType.English, "Godlike" },
            { LangType.Turkey, "Tanrısal" }
        };

    private static readonly IReadOnlyDictionary<LangType, string> Unknown =
        new Dictionary<LangType, string>
        {
            { LangType.Russian, "Unknown" },
            { LangType.English, "Unknown" },
            { LangType.Turkey, "Bilinmeyen" }
        };
}