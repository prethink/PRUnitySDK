using YG;

public class YandexPlayerNameService : PlayerNameServiceBase
{
    public override string GetCurrentName()
    {
        var currentName = YG2.player.name;
        if (currentName != null && currentName != "unauthorized")
            return currentName;

        return PlayerUtils.GetDefaultName();
    }
}
