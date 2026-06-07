using System.Collections.Generic;

public class QualityLocalizationProvider : ILocalizationProvider
{
    private readonly QualityType quality;

    public QualityLocalizationProvider(QualityType quality)
    {
        this.quality = quality;
    }

    public string LocalizationKey => $"quality_{quality.ToString().ToLower()}";

    public IReadOnlyList<string> LocalizationValues => Get(quality);

    public static IReadOnlyList<string> Get(QualityType type)
    {
        return type switch
        {
            QualityType.Common => new[] { "Обычный", "Common", "Sıradan" },
            QualityType.Uncommon => new[] { "Необычный", "Uncommon", "Sıradışı" },
            QualityType.Rare => new[] { "Редкий", "Rare", "Nadir" },
            QualityType.Mythic => new[] { "Мифический", "Mythic", "Efsanevi" },
            QualityType.Epic => new[] { "Эпический", "Epic", "Epik" },
            QualityType.Legendary => new[] { "Легендарный", "Legendary", "Efsanevi" },
            QualityType.Ancient => new[] { "Древний", "Ancient", "Kadim" },
            QualityType.Godlike => new[] { "Божественный", "Godlike", "Tanrısal" },

            _ => new[] { "Unknown", "Unknown", "Bilinmeyen" }
        };
    }
}