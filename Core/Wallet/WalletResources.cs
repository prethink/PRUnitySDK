public class WalletResources
{
    public long GetBalance(ResourceItemData resource)
    {
        return WalletService.Instance.GetBalance(resource.Name);
    }

    public void Add(ResourceItemData resource, long amount, bool save = true)
    {
        WalletService.Instance.Add(resource.Name, amount, save);
    }

    public bool Buy(ResourceItemData resource, long amount)
    {
        var result = WalletService.Instance.Buy(resource.Name, amount);
        return result;
    }

    public bool CanBuy(ResourceItemData resource, long amount)
    {
        return WalletService.Instance.CanBuy(resource.Name, amount);
    }
}
