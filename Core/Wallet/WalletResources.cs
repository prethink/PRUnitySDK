public class WalletResources
{
    public long GetBalance(ResourceItemDefinitionBase resource)
    {
        return WalletService.Instance.GetBalance(resource.Name);
    }

    public void Add(ResourceItemDefinitionBase resource, long amount, bool save = true)
    {
        WalletService.Instance.Add(resource.Name, amount, save);
    }

    public bool Buy(ResourceItemDefinitionBase resource, long amount)
    {
        var result = WalletService.Instance.Buy(resource.Name, amount);
        return result;
    }

    public bool CanBuy(ResourceItemDefinitionBase resource, long amount)
    {
        return WalletService.Instance.CanBuy(resource.Name, amount);
    }
}
