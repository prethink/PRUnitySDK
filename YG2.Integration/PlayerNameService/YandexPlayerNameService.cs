#if !PRSDK_DISABLE_YG2
using YG;

public class YandexPlayerNameService : PlayerNameServiceBase
{
    /// <summary>
    /// Так плагин называет игрока, который не вошёл в аккаунт.
    /// </summary>
    private const string UnauthorizedName = "unauthorized";

    /// <inheritdoc />
    /// <remarks>
    /// Именем не считаются три ответа площадки: «unauthorized» — игрок не вошёл, «anonymous» —
    /// скрыл имя, и пустая строка — вошёл, но доступа к имени не дал. Последний случай самый
    /// частый; пропущенная пустая строка оставляла игрока без имени в ленте сообщений.
    /// </remarks>
    protected override bool TryGetPlatformName(out string name)
    {
        name = YG2.player.name;

        return !string.IsNullOrWhiteSpace(name) && name != UnauthorizedName && name != InfoYG.ANONYMOUS;
    }
}
#endif
