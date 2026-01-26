public class WalletService : SingletonProviderBase<WalletService>
{
    public long GetBalance(string currency)
    {
        return GetManager().TryGetLong(currency, out var balance) 
            ? balance
            : 0;
    }

    public void Add(string currency, long amount, bool save = true)
    {
        if(!GetManager().TryGetLong(currency, out var balance))
        {
            GetManager().SetLong(currency, amount, save);
            return;
        }

        GetManager().SetLong(currency, balance + amount, save);
    }

    public bool Buy(string currency, long amount)
    {
        if (!CanBuy(currency, amount))
            return false;

        var currentBalance = GetBalance(currency);
        currentBalance -= amount;
        GetManager().SetLong(currency, currentBalance);
        return true;
    }

    public bool CanBuy(string currency, long amount)
    {
        var currentBalance = GetBalance(currency);
        return currentBalance >= amount;
    }

    private ProjectPropertiesManager GetManager()
    {
        return PRUnitySDK.Managers.ProjectPropertiesManager;
    }
}
