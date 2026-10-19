#if !PRSDK_DISABLE_VK
/// <summary>
/// Имя игрока из профиля ВКонтакте или Одноклассников.
/// </summary>
public class VKPlayerNameService : PlayerNameServiceBase
{
    /// <inheritdoc />
    /// <remarks>
    /// Только имя, без фамилии: фамилию в игре показывать незачем.
    /// </remarks>
    protected override bool TryGetPlatformName(out string name)
    {
        name = VKBridge.Launch?.firstName;
        return !string.IsNullOrWhiteSpace(name);
    }
}
#endif
