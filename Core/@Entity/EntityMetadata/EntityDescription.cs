using System;
using UnityEngine;

public class EntityDescription
{
    public IEntityMetadata Base { get; }
    public IEntityMetadata Override { get; }

    public Func<Sprite> SpriteOverride { get; private set; }
    public Func<string> NameOverride { get; private set; }
    public Func<string> LocalizationOverride { get; private set; }

    /// <summary>
    /// Переопределение источника перевода подписи.
    /// </summary>
    public Func<ILocalizationProvider> LocalizationProviderOverride { get; private set; }

    public Func<QualityType> QualityOverride { get; private set; }

    public EntityDescription(IEntityMetadata baseInfo, IEntityMetadata overrideInfo = null)
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

        return GetLocalizationProvider()?.GetTranslate();
    }

    /// <summary>
    /// Источник перевода подписи.
    /// </summary>
    /// <remarks>
    /// Нужен там, где подпись отдают в <c>SetLocalization</c>: готовая строка из
    /// <see cref="GetLocalization"/> живёт до первой смены языка, а провайдер
    /// <c>LocalizationObserver</c> перечитывает сам. Порядок тот же, что и у остальных
    /// частей описания: переопределение, затем <see cref="Override"/>, затем
    /// <see cref="Base"/>.
    /// </remarks>
    /// <returns>Провайдер перевода либо <c>null</c>, если описания нет вовсе.</returns>
    public ILocalizationProvider GetLocalizationProvider()
    {
        if (LocalizationProviderOverride != null)
            return LocalizationProviderOverride();

        if (Override != null)
            return Override;

        return Base;
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

        return Base != null ? Base.Quality : QualityType.Common;
    }

    public void SetNameOverride(Func<string> func) => NameOverride = func;
    public void SetLocalizationOverride(Func<string> func) => LocalizationOverride = func;
    public void SetLocalizationProviderOverride(Func<ILocalizationProvider> func) => LocalizationProviderOverride = func;
    public void SetSpriteOverride(Func<Sprite> func) => SpriteOverride = func;
    public void SetQualityOverride(Func<QualityType> func) => QualityOverride = func;

    public IEntityMetadata GetMetadata()
    {
        return Override != null 
            ? Override 
            : Base;
    }
}