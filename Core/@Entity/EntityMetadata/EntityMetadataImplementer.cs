using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Описание сущности, собранное в рантайме.
/// </summary>
public class EntityMetadataImplementer : IEntityMetadata
{
    public Guid TypeGuid { get; private set; }
    public string Name { get; private set; }
    public Sprite Icon { get; private set; }

    public string LocalizationKey { get; private set; }

    public QualityType Quality { get; private set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues { get; }

    /// <summary>
    /// Модификаторы характеристик, которые описание добавляет сущности.
    /// </summary>
    public IEnumerable<StatModifier> StatModifiers { get; } = new List<StatModifier>();

    public EntityMetadataImplementer(
        Guid type,
        string name,
        Sprite icon,
        string localizationKey,
        Dictionary<LangType, string> localizationValues)
    {
        TypeGuid = type;
        Name = name;
        Icon = icon;

        LocalizationKey = localizationKey;
        LocalizationValues = localizationValues;
}

    public void SetQuality(QualityType quality)
    {
        Quality = quality;
    }
}