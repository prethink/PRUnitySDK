using System;
using UnityEngine;

public class EntityInfoContainer
{
    public IEntityInfo Base { get; }
    public IEntityInfo Override { get; }

    public Func<Sprite> SpriteOverride { get; private set; }
    public Func<string> NameOverride { get; private set; }
    public Func<string> LocalizationOverride { get; private set; }

    public Func<QualityType> QualityOverride { get; private set; }

    public EntityInfoContainer(IEntityInfo baseInfo, IEntityInfo overrideInfo = null)
    {
        Base = baseInfo;
        Override = overrideInfo;
    }

    public string GetName()
    {
        if (NameOverride != null)
            return NameOverride();

        if (Override != null)
            return Override.Name;

        return Base?.Name;
    }

    public string GetLocalization()
    {
        if (LocalizationOverride != null)
            return LocalizationOverride();

        if (Override != null)
            return Override.GetTranslate();

        return Base?.GetTranslate();
    }

    public Sprite GetIcon()
    {
        if (SpriteOverride != null)
            return SpriteOverride();

        if (Override != null)
            return Override.Icon;

        return Base?.Icon;
    }

    public QualityType GetQuality()
    {
        if (QualityOverride != null)
            return QualityOverride();

        if (Override != null)
            return Override.Quality;

        return Base.Quality;
    }

    public void SetNameOverride(Func<string> func) => NameOverride = func;
    public void SetLocalizationOverride(Func<string> func) => LocalizationOverride = func;
    public void SetSpriteOverride(Func<Sprite> func) => SpriteOverride = func;
    public void SetQualityOverride(Func<QualityType> func) => QualityOverride = func;
}