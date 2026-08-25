public class WalletService : SingletonProviderBase<WalletService>
{
    public long GetBalance(Enumeration currency)
    {
        return GetManager().GetResource(currency);
    }

    public void Add(Enumeration currency, long amount, bool save = true)
    {
        GetManager().AddResourceValue(
            currency,
            amount,
            requiredNotify: true,
            requiredSaveNow: save);
    }

    public bool Buy(Enumeration currency, long amount)
    {
        return GetManager().TrySpendResource(
            currency,
            amount,
            requiredNotify: true,
            requiredSaveNow: true);
    }

    public bool CanBuy(Enumeration currency, long amount)
    {
        if (amount < 0)
            return false;

        var currentBalance = GetBalance(currency);
        return currentBalance >= amount;
    }

    private ResourceManager GetManager()
    {
        return PRUnitySDK.Managers.Resource;
    }
}
