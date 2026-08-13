public class WalletResources
{
    public long GetBalance(ResourceItemDefinition resource)
    {
        return TryGetCurrency(resource, out Enumeration currency)
            ? WalletService.Instance.GetBalance(currency)
            : 0;
    }

    public void Add(ResourceItemDefinition resource, long amount, bool save = true)
    {
        if (!TryGetCurrency(resource, out Enumeration currency))
        {
            PRLog.WriteWarning(typeof(WalletResources), "Cannot add resource: ResourceItemDefinition or CurrencyType is not configured.");
            return;
        }

        WalletService.Instance.Add(currency, amount, save);
    }

    public bool Buy(ResourceItemDefinition resource, long amount)
    {
        if (!TryGetCurrency(resource, out Enumeration currency))
        {
            PRLog.WriteWarning(typeof(WalletResources), "Cannot buy resource: ResourceItemDefinition or CurrencyType is not configured.");
            return false;
        }

        return WalletService.Instance.Buy(currency, amount);
    }

    public bool CanBuy(ResourceItemDefinition resource, long amount)
    {
        return TryGetCurrency(resource, out Enumeration currency) &&
               WalletService.Instance.CanBuy(currency, amount);
    }

    /// <summary>
    /// Проверяет, что ресурс содержит корректно настроенный тип валюты.
    /// </summary>
    public bool IsConfigured(ResourceItemDefinition resource)
        => TryGetCurrency(resource, out _);

    private static bool TryGetCurrency(ResourceItemDefinition resource, out Enumeration currency)
    {
        currency = resource?.CurrencyType?.ToEnumeration();
        return currency != null;
    }
}
