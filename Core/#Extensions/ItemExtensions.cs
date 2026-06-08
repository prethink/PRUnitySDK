public static class ItemExtensions
{
    public static string GetName(this IItemDefinition itemDefinition)
    {
        if(itemDefinition is ILocalizationProvider localizationProvider)
            return PRLocalization.GetTranslate(localizationProvider);

        return itemDefinition.Name;
    }

    public static string GetName(this IItemDefinition itemDefinition, LangType langType)
    {
        if (itemDefinition is ILocalizationProvider localizationProvider)
            return PRLocalization.GetTranslate(localizationProvider, langType);

        return itemDefinition.Name;
    }
}
