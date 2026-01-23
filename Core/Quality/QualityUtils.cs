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
        QualityType.Common => PRUnitySDK.Settings.Quality.CommonColor,
        QualityType.Uncommon => PRUnitySDK.Settings.Quality.UncommonColor,
        QualityType.Rare => PRUnitySDK.Settings.Quality.RareColor,
        QualityType.Epic => PRUnitySDK.Settings.Quality.EpicColor,
        QualityType.Legendary => PRUnitySDK.Settings.Quality.LegendaryColor,
        QualityType.Mythic => PRUnitySDK.Settings.Quality.MythicColor,
        QualityType.Ancient => PRUnitySDK.Settings.Quality.AncientColor,
        QualityType.Godlike => PRUnitySDK.Settings.Quality.GodlikeColor,
        _ => Color.white
    };

    /// <summary>
    /// Получить качество по случайному весу.
    /// </summary>
    /// <returns>Тип качества.</returns>
    public static QualityType GetQualityByRandomWeights()
    {
        return WeightUtils.GetRandomWeight(GetWeights().Cast<WeightItem<QualityType>>().ToList());
    }
}
