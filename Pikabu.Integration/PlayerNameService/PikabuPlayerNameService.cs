#if !PRSDK_DISABLE_PIKABU
/// <summary>
/// Имя игрока из аккаунта Пикабу.
/// </summary>
public class PikabuPlayerNameService : PlayerNameServiceBase
{
    /// <inheritdoc />
    /// <remarks>
    /// У гостя имени нет: тогда остаётся имя по умолчанию.
    /// </remarks>
    protected override bool TryGetPlatformName(out string name)
    {
        PikabuPlayer player = PikabuSDK.Player;
        name = player is { isAuthorized: true } ? player.name : null;

        return !string.IsNullOrWhiteSpace(name);
    }
}
#endif
