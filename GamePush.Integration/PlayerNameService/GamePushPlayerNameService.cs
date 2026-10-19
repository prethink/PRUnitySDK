#if !PRSDK_DISABLE_GAMEPUSH
using GamePush;

/// <summary>
/// Имя игрока из профиля GamePush.
/// </summary>
public class GamePushPlayerNameService : PlayerNameServiceBase
{
    /// <inheritdoc />
    /// <remarks>
    /// Пустое имя — игрок не вошёл или не дал площадке имя: тогда остаётся имя по умолчанию.
    /// </remarks>
    protected override bool TryGetPlatformName(out string name)
    {
        name = GP_Init.isReady ? GP_Player.GetName() : null;

        return !string.IsNullOrWhiteSpace(name);
    }
}
#endif
