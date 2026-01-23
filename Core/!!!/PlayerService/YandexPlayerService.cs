using YG;

public class YandexPlayerService : PlayerServiceBase
{
    public override string GetCurrentName()
    {
        var currentName = YG2.player.name;
        if (currentName != null && currentName != "unauthorized")
            return currentName;

        return PlayerUtils.GetDefaultName();
    }
}
