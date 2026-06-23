public class WalletResources
{
    public long GetBalance(ResourceItemDefinition resource)
    {
        return WalletService.Instance.GetBalance(resource.CurrencyType.ToEnumeration());
    }

    public void Add(ResourceItemDefinition resource, long amount, bool save = true)
    {
        WalletService.Instance.Add(resource.CurrencyType.ToEnumeration(), amount, save);
    }

    public bool Buy(ResourceItemDefinition resource, long amount)
    {
        var result = WalletService.Instance.Buy(resource.CurrencyType.ToEnumeration(), amount);
        return result;
    }

    public bool CanBuy(ResourceItemDefinition resource, long amount)
    {
        return WalletService.Instance.CanBuy(resource.CurrencyType.ToEnumeration(), amount);
    }
}
