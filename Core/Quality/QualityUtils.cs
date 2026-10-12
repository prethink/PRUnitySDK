using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Утилиты для работы с качеством предметов.
/// </summary>
public static class QualityUtils
{
    /// <summary>
    /// Получить настроенный вес указанного качества.
    /// </summary>
    public static ulong GetWeight(QualityType quality) => quality switch
    {
        QualityType.Common => PRUnitySDK.Settings.Quality.CommonWeight,
        QualityType.Uncommon => PRUnitySDK.Settings.Quality.UncommonWeight,
        QualityType.Rare => PRUnitySDK.Settings.Quality.RareWeight,
        QualityType.Epic => PRUnitySDK.Settings.Quality.EpicWeight,
        QualityType.Legendary => PRUnitySDK.Settings.Quality.LegendaryWeight,
        QualityType.Mythic => PRUnitySDK.Settings.Quality.MythicWeight,
        QualityType.Ancient => PRUnitySDK.Settings.Quality.AncientWeight,
        QualityType.Godlike => PRUnitySDK.Settings.Quality.GodlikeWeight,
        QualityType.Cosmic => PRUnitySDK.Settings.Quality.CosmicWeight,
        QualityType.Secret => PRUnitySDK.Settings.Quality.SecretWeight,
        QualityType.Rainbow => PRUnitySDK.Settings.Quality.RainbowWeight,
        _ => 0
    };

    /// <summary>
    /// Получить веса всех качеств.
    /// </summary>
    public static List<QualityWeight> GetWeights()
    {
        return new List<QualityWeight>
        {
            new QualityWeight { Item = QualityType.Common, Weight = GetWeight(QualityType.Common) },
            new QualityWeight { Item = QualityType.Uncommon, Weight = GetWeight(QualityType.Uncommon) },
            new QualityWeight { Item = QualityType.Rare, Weight = GetWeight(QualityType.Rare) },
            new QualityWeight { Item = QualityType.Epic, Weight = GetWeight(QualityType.Epic) },
            new QualityWeight { Item = QualityType.Legendary, Weight = GetWeight(QualityType.Legendary) },
            new QualityWeight { Item = QualityType.Mythic, Weight = GetWeight(QualityType.Mythic) },
            new QualityWeight { Item = QualityType.Ancient, Weight = GetWeight(QualityType.Ancient) },
            new QualityWeight { Item = QualityType.Godlike, Weight = GetWeight(QualityType.Godlike) },
            new QualityWeight { Item = QualityType.Cosmic, Weight = GetWeight(QualityType.Cosmic) },
            new QualityWeight { Item = QualityType.Secret, Weight = GetWeight(QualityType.Secret) },
            new QualityWeight { Item = QualityType.Rainbow, Weight = GetWeight(QualityType.Rainbow) }
        };
    }

    /// <summary>
    /// Получить цвет качества предмета.
    /// </summary>
    public static Color GetColor(QualityType quality) => quality switch
    {
        QualityType.Common => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.CommonColor,
        QualityType.Uncommon => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.UncommonColor,
        QualityType.Rare => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.RareColor,
        QualityType.Epic => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.EpicColor,
        QualityType.Legendary => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.LegendaryColor,
        QualityType.Mythic => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.MythicColor,
        QualityType.Ancient => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.AncientColor,
        QualityType.Godlike => PRUnitySDK.Settings.Quality.UseDefaultColor ? GetDefaultColor(quality) : PRUnitySDK.Settings.Quality.GodlikeColor,
        QualityType.Cosmic => SettingsOrDefault(quality, PRUnitySDK.Settings.Quality.CosmicColor),
        QualityType.Secret => SettingsOrDefault(quality, PRUnitySDK.Settings.Quality.SecretColor),
        QualityType.Rainbow => SettingsOrDefault(quality, PRUnitySDK.Settings.Quality.RainbowColor),
        _ => Color.white
    };

    /// <summary>
    /// Получить стандартный цвет качества.
    /// </summary>
    public static Color GetDefaultColor(QualityType quality) => quality switch
    {
        QualityType.Common => new Color(0.75f, 0.75f, 0.75f),
        QualityType.Uncommon => new Color(0.35f, 0.85f, 0.35f),
        QualityType.Rare => new Color(0.35f, 0.55f, 0.95f),
        QualityType.Mythic => new Color(0.75f, 0.35f, 0.95f),
        QualityType.Epic => new Color(0.90f, 0.30f, 0.60f),
        QualityType.Legendary => new Color(1.00f, 0.65f, 0.10f),
        QualityType.Ancient => new Color(0.85f, 0.25f, 0.15f),
        QualityType.Godlike => new Color(1.00f, 0.85f, 0.25f),
        QualityType.Cosmic => new Color(0.55f, 0.20f, 0.85f),
        QualityType.Secret => new Color(0.95f, 0.95f, 0.95f),
        QualityType.Rainbow => new Color(0.45f, 0.90f, 0.90f),
        _ => Color.white
    };

    // Цвет новых качеств в настройках уже выпущенных проектов пуст (прозрачный чёрный):
    // такой цвет считается незаданным.
    private static Color SettingsOrDefault(QualityType quality, Color configured) =>
        PRUnitySDK.Settings.Quality.UseDefaultColor || configured.a <= 0f ? GetDefaultColor(quality) : configured;

    /// <summary>
    /// Получить модификатор уровня указанного качества.
    /// </summary>
    public static long GetQualityLevelModifier(QualityType quality) => quality switch
    {
        QualityType.Common => PRUnitySDK.Settings.Quality.CommonLevel,
        QualityType.Uncommon => PRUnitySDK.Settings.Quality.UncommonLevel,
        QualityType.Rare => PRUnitySDK.Settings.Quality.RareLevel,
        QualityType.Epic => PRUnitySDK.Settings.Quality.EpicLevel,
        QualityType.Legendary => PRUnitySDK.Settings.Quality.LegendaryLevel,
        QualityType.Mythic => PRUnitySDK.Settings.Quality.MythicLevel,
        QualityType.Ancient => PRUnitySDK.Settings.Quality.AncientLevel,
        QualityType.Godlike => PRUnitySDK.Settings.Quality.GodlikeLevel,
        QualityType.Cosmic => PRUnitySDK.Settings.Quality.CosmicLevel,
        QualityType.Secret => PRUnitySDK.Settings.Quality.SecretLevel,
        QualityType.Rainbow => PRUnitySDK.Settings.Quality.RainbowLevel,
        _ => 0
    };

    /// <summary>
    /// Случайно выбрать качество внутри диапазона.
    /// </summary>
    public static QualityType GetQualityByRandomWeights(QualityRange range)
    {
        var filtered = GetWeights()
            .Where(weight => range.Contains(weight.Item))
            .Cast<WeightItem<QualityType>>()
            .ToList();

        return WeightUtils.GetRandomWeight(filtered);
    }

    /// <summary>
    /// Случайно выбрать качество по глобальным весам.
    /// </summary>
    public static QualityType GetQualityByRandomWeights()
    {
        return WeightUtils.GetRandomWeight(GetWeights().Cast<WeightItem<QualityType>>().ToList());
    }
}
