public abstract class PlayerNameServiceBase : INameProvider
{
    #region INameProvider

    public string Name => GetCurrentName();

    #endregion

    public abstract string GetCurrentName();
}
