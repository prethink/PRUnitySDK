/// <summary>
/// –асширение дл€ перечислени€ QualityType, позвол€ющее сравнивать его элементы по пор€дку.
/// </summary>
public static class QualityExtension
{
    /// <summary>
    /// ќпредел€ет, €вл€етс€ ли текущий тип качества выше другого типа качества.
    /// </summary>
    /// <param name="currentType">“екущий тип качества.</param>
    /// <param name="anotherType">“ип качества дл€ сравнени€.</param>
    /// <returns>¬озвращает true, если текущий тип качества выше, чем переданный, иначе false.</returns>
    public static bool IsHigher(this QualityType currentType, QualityType anotherType)
    {
        // —равниваем пор€дковые значени€ типов качества
        return (int)currentType > (int)anotherType;
    }

    public static string GetTranslate(this QualityType qualityType, LangType langType)
    {
        return new QualityLocalizationProvider(qualityType).GetTranslate(langType);
    }
}
