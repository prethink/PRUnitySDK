public abstract class WalletBase 
{
    public abstract string Currency { get; }

    public virtual long GetBalance()
    {
        return WalletService.Instance.GetBalance(Currency);
    }

    public virtual void Add(long amount, bool save = true)
    {
        WalletService.Instance.Add(Currency, amount, save);
    }

    public virtual bool Buy(long amount)
    {
        var result = WalletService.Instance.Buy(Currency, amount);
        return result;
    }

    public virtual bool CanBuy(long amount)
    {
        return WalletService.Instance.CanBuy(Currency, amount);
    }
}
