public class LocalPlayerNameService : PlayerNameServiceBase
{
    public override string GetCurrentName()
    {
        return PlayerUtils.GetDefaultName();
    }
}
