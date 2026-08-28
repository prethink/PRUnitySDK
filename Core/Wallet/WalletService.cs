public class WalletService : SingletonProviderBase<WalletService>
{
    public long GetBalance(Enumeration currency)
    {
        return GetManager().GetResource(currency);
    }

    /// <summary>
    /// Начисляет валюту.
    /// </summary>
    /// <param name="save">Запустить сохранение после начисления.</param>
    /// <param name="ignoreSaveCooldown">Записать данные не дожидаясь кулдауна сохранения.</param>
    public void Add(Enumeration currency, long amount, bool save = true, bool ignoreSaveCooldown = false)
    {
        GetManager().AddResourceValue(
            currency,
            amount,
            requiredNotify: true,
            requiredSaveNow: save,
            ignoreSaveCooldown: ignoreSaveCooldown);
    }

    /// <summary>
    /// Списывает валюту, если её хватает.
    /// </summary>
    /// <remarks>
    /// Решение об обходе кулдауна принимает вызывающий: только он знает, чем именно
    /// расплачивается игрок. Разовая покупка предмета должна лечь на диск сразу, а трата
    /// в цикле — например, ставка за каждый бросок — подождёт общего расписания, иначе
    /// каждая мелочь дёргала бы запись.
    /// </remarks>
    /// <param name="ignoreSaveCooldown">Записать данные не дожидаясь кулдауна сохранения.</param>
    public bool Buy(Enumeration currency, long amount, bool ignoreSaveCooldown = false)
    {
        return GetManager().TrySpendResource(
            currency,
            amount,
            requiredNotify: true,
            requiredSaveNow: true,
            ignoreSaveCooldown: ignoreSaveCooldown);
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
