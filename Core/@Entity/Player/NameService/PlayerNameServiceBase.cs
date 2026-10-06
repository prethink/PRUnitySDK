public abstract class PlayerNameServiceBase : INameProvider
{
    #region INameProvider

    public string Name => GetCurrentName();

    #endregion

    private string cachedName;

    private ILocalizationProvider cachedProvider;

    /// <summary>
    /// Имя игрока строкой: настоящее либо запасное на текущем языке.
    /// </summary>
    /// <remarks>
    /// Запасное имя в строке застывает на языке вызова. Там, где подпись живёт на экране
    /// дольше кадра, бери <see cref="GetNameProvider"/>.
    /// </remarks>
    public virtual string GetCurrentName()
    {
        return TryGetPlatformName(out string name) ? name : PlayerUtils.GetDefaultName();
    }

    /// <summary>
    /// Имя игрока для подписи: настоящее — одной строкой на всех языках, запасное — переводимое.
    /// </summary>
    public ILocalizationProvider GetNameProvider()
    {
        if (!TryGetPlatformName(out string name))
            return PlayerUtils.DefaultName;

        // Имя спрашивают часто, а меняется оно редко: провайдер пересобирается только вместе с ним.
        if (cachedProvider == null || cachedName != name)
        {
            cachedName = name;
            cachedProvider = new LocalizationProvider(name);
        }

        return cachedProvider;
    }

    /// <summary>
    /// Настоящее имя игрока с платформы.
    /// </summary>
    /// <returns><c>false</c>, если имени нет: платформа его не отдала или игрок его скрыл.</returns>
    protected virtual bool TryGetPlatformName(out string name)
    {
        name = null;
        return false;
    }
}
