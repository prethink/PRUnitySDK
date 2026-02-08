using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Утилиты для работы с качеством предметов.
/// </summary>
public static class QualityUtils
{
    /// <summary>
    /// Получить веса для качества предметов.
    /// </summary>
    /// <returns></returns>
    public static List<QualityWeight> GetWeights()
    {
        return new List<QualityWeight>
        {
            new QualityWeight { Item = QualityType.Common, Weight = PRUnitySDK.Settings.Quality.CommonWeight },
            new QualityWeight { Item = QualityType.Uncommon, Weight = PRUnitySDK.Settings.Quality.UncommonWeight },
            new QualityWeight { Item = QualityType.Rare, Weight = PRUnitySDK.Settings.Quality.RareWeight },
            new QualityWeight { Item = QualityType.Epic, Weight = PRUnitySDK.Settings.Quality.EpicWeight },
            new QualityWeight { Item = QualityType.Legendary, Weight = PRUnitySDK.Settings.Quality.LegendaryWeight },
            new QualityWeight { Item = QualityType.Mythic, Weight = PRUnitySDK.Settings.Quality.MythicWeight },
            new QualityWeight { Item = QualityType.Ancient, Weight = PRUnitySDK.Settings.Quality.AncientWeight },
            new QualityWeight { Item = QualityType.Godlike, Weight = PRUnitySDK.Settings.Quality.GodlikeWeight }
        };
    }

    /// <summary>
    /// Получить цвет качества предмета.
    /// </summary>
    /// <param name="quality">Качество.</param>
    /// <returns>Цвет.</returns>
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
        _ => Color.white
    };

    /// <summary>
    /// Цвет по умолчанию для качества предмета, если в настройках не задано.
    /// </summary>
    /// <param name="quality">Качество предмета.</param>
    /// <returns>Цвет.</returns>
    public static Color GetDefaultColor(QualityType quality)
    {
        return quality switch
        {
            QualityType.Common => new Color(0.75f, 0.75f, 0.75f), // серый
            QualityType.Uncommon => new Color(0.35f, 0.85f, 0.35f), // зелёный
            QualityType.Rare => new Color(0.35f, 0.55f, 0.95f), // синий
            QualityType.Mythic => new Color(0.75f, 0.35f, 0.95f), // фиолетовый
            QualityType.Epic => new Color(0.90f, 0.30f, 0.60f), // эпик (розовый)
            QualityType.Legendary => new Color(1.00f, 0.65f, 0.10f), // оранжевый
            QualityType.Ancient => new Color(0.85f, 0.25f, 0.15f), // древний
            QualityType.Godlike => new Color(1.00f, 0.85f, 0.25f), // золото

            _ => Color.white
        };
    }

    /// <summary>
    /// Получить качество по случайному весу.
    /// </summary>
    /// <returns>Тип качества.</returns>
    public static QualityType GetQualityByRandomWeights()
    {
        return WeightUtils.GetRandomWeight(GetWeights().Cast<WeightItem<QualityType>>().ToList());
    }
}
