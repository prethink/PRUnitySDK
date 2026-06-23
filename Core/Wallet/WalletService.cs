public class WalletService : SingletonProviderBase<WalletService>
{
    public long GetBalance(Enumeration currency)
    {
        return GetManager().GetOrCreateResource(currency);
    }

    public void Add(Enumeration currency, long amount, bool save = true)
    {
        GetManager().SetOrUpdateResource(currency, GetBalance(currency) + amount, save);
    }

    public bool Buy(Enumeration currency, long amount)
    {
        if (!CanBuy(currency, amount))
            return false;

        var currentBalance = GetBalance(currency);
        currentBalance -= amount;
        GetManager().SetOrUpdateResource(currency, currentBalance);
        return true;
    }

    public bool CanBuy(Enumeration currency, long amount)
    {
        var currentBalance = GetBalance(currency);
        return currentBalance >= amount;
    }

    private ResourceManager GetManager()
    {
        return PRUnitySDK.Managers.ResourceManager;
    }
}
