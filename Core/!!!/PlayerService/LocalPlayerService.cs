public class LocalPlayerService : PlayerServiceBase
{
    public override string GetCurrentName()
    {
        return PlayerUtils.GetDefaultName();
    }
}
