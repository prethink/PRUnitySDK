public static class LevelExtensions
{
    public static long GetLevelDefinition(this ILevelProvider levelProvider, long currentLevel)
    {
        if(levelProvider is IQualityProvider qualityProvider && PRUnitySDK.Settings.Quality.UseQualityLevelModifier)
            currentLevel += QualityUtils.GetQualityLevelModifier(qualityProvider.Quality);

        return currentLevel;
    }
}
