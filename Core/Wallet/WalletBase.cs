public abstract class WalletBase 
{
    public abstract Enumeration Currency { get; }

    public virtual long GetBalance()
    {
        return WalletService.Instance.GetBalance(Currency);
    }

    public virtual void Add(long amount, bool save = true, bool ignoreSaveCooldown = false)
    {
        WalletService.Instance.Add(Currency, amount, save, ignoreSaveCooldown);
    }

    /// <param name="ignoreSaveCooldown">Записать данные не дожидаясь кулдауна сохранения.</param>
    public virtual bool Buy(long amount, bool ignoreSaveCooldown = false)
    {
        var result = WalletService.Instance.Buy(Currency, amount, ignoreSaveCooldown);
        return result;
    }

    public virtual bool CanBuy(long amount)
    {
        return WalletService.Instance.CanBuy(Currency, amount);
    }
}
