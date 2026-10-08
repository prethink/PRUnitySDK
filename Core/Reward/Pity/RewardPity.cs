using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Розыгрыш с гарантом и счётчик прокруток без редкой награды.
/// </summary>
/// <remarks>
/// Розыгрыш и счётчик разделены намеренно. Выбрать награду и выдать её — разные моменты:
/// кейс выбирает итог до прокрутки ленты, а выдаёт после неё, и закрытое на середине окно
/// не должно сдвигать счётчик. Поэтому <see cref="TryRoll"/> счётчик только читает,
/// а двигает его <see cref="Register"/> — когда награда действительно досталась игроку.
/// </remarks>
public static class RewardPity
{
    /// <summary>
    /// Начало ключа счётчика в свойствах проекта; дальше идёт идентификатор розыгрыша.
    /// </summary>
    private const string KeyPrefix = "RewardPity.";

    /// <summary>
    /// Награда считается редкой по этим настройкам.
    /// </summary>
    public static bool IsRare(RewardPitySettings settings, RewardBase reward)
    {
        return settings != null && reward != null && reward.QualityReward >= settings.RareQuality;
    }

    /// <summary>
    /// Сколько прокруток подряд прошло без редкой награды.
    /// </summary>
    /// <param name="id">Идентификатор розыгрыша: свой у колеса и у каждого кейса.</param>
    public static long GetMisses(string id)
    {
        return string.IsNullOrEmpty(id)
            ? 0
            : Math.Max(0, ProjectPropertiesManager.Instance.GetLong(KeyPrefix + id));
    }

    /// <summary>
    /// Через сколько прокруток редкая награда гарантирована, считая ту, что её выдаст.
    /// </summary>
    /// <returns>Ноль, если гарант выключен или сейчас не действует.</returns>
    public static int GetSpinsToGuarantee(IReadOnlyList<WeightedRewardEntry> rewards, Predicate<RewardBase> filter,
        RewardPitySettings settings, string id)
    {
        return IsActive(rewards, filter, settings)
            ? (int)Math.Max(1, settings.GuaranteeAfter - GetMisses(id))
            : 0;
    }

    /// <summary>
    /// Разыгрывает награду по весам с учётом гаранта. Счётчик не меняет.
    /// </summary>
    /// <param name="rewards">Награды и их веса.</param>
    /// <param name="filter">Какие награды сейчас можно разыграть; <c>null</c> — все.</param>
    /// <param name="settings">Настройки гаранта; <c>null</c> или выключенные — обычный розыгрыш.</param>
    /// <param name="misses">Сколько прокруток подряд прошло без редкой награды.</param>
    /// <returns><see langword="false"/>, если разыгрывать нечего.</returns>
    public static bool TryRoll(IReadOnlyList<WeightedRewardEntry> rewards, Predicate<RewardBase> filter,
        RewardPitySettings settings, long misses, out RewardBase reward)
    {
        reward = null;

        if (rewards == null)
            return false;

        bool active = IsActive(rewards, filter, settings);
        bool guaranteed = active && misses + 1 >= settings.GuaranteeAfter;
        double boost = active ? 1d + settings.ChanceGrowth * Math.Max(0, misses) : 1d;

        double total = 0d;

        foreach (WeightedRewardEntry entry in rewards)
            total += GetWeight(entry, filter, settings, active, guaranteed, boost);

        if (total <= 0d)
            return false;

        double point = UnityEngine.Random.value * total;

        foreach (WeightedRewardEntry entry in rewards)
        {
            double weight = GetWeight(entry, filter, settings, active, guaranteed, boost);

            if (weight <= 0d)
                continue;

            // Последняя годная запоминается: случайное число может прийти ровно единицей,
            // и точка тогда лежит на самой границе суммы.
            reward = entry.Item;

            if (point < weight)
                return true;

            point -= weight;
        }

        return reward != null;
    }

    /// <summary>
    /// Шанс награды в следующем розыгрыше, от нуля до единицы, с учётом гаранта.
    /// </summary>
    /// <remarks>
    /// Тот же расчёт, что в <see cref="TryRoll"/>: подпись «12%» рядом с наградой обещает ровно то,
    /// что разыграет следующая прокрутка. Награда, стоящая в списке несколько раз, получает сумму.
    /// </remarks>
    public static double GetChance(IReadOnlyList<WeightedRewardEntry> rewards, Predicate<RewardBase> filter,
        RewardPitySettings settings, long misses, RewardBase reward)
    {
        if (rewards == null || reward == null)
            return 0d;

        bool active = IsActive(rewards, filter, settings);
        bool guaranteed = active && misses + 1 >= settings.GuaranteeAfter;
        double boost = active ? 1d + settings.ChanceGrowth * Math.Max(0, misses) : 1d;

        double total = 0d;
        double own = 0d;

        foreach (WeightedRewardEntry entry in rewards)
        {
            double weight = GetWeight(entry, filter, settings, active, guaranteed, boost);
            total += weight;

            if (weight > 0d && entry.Item == reward)
                own += weight;
        }

        return total > 0d ? own / total : 0d;
    }

    /// <summary>
    /// Шанс в процентах для подписи: «39%», «5%», «0.5%».
    /// </summary>
    /// <remarks>
    /// Меньше процента — с десятыми: округлённая до нуля редкая награда читалась бы как
    /// «не выпадает».
    /// </remarks>
    public static string FormatChance(double chance)
    {
        double percent = chance * 100d;

        if (percent >= 1d)
            return percent.ToString("0", CultureInfo.InvariantCulture) + "%";

        return percent.ToString(percent >= 0.1d ? "0.#" : "0.##", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>
    /// Учитывает выданную награду: редкая обнуляет счётчик, обычная прибавляет к нему.
    /// </summary>
    /// <param name="save">Сохранить сразу. Без сохранения счётчик запишет следующее сохранение.</param>
    public static void Register(RewardPitySettings settings, string id, RewardBase reward, bool save = true)
    {
        if (settings == null || !settings.Enabled || string.IsNullOrEmpty(id) || reward == null)
            return;

        // Выше гарантии счётчик не растёт: редких наград могло временно не быть,
        // и набежавшее за это время не должно копиться впрок.
        long misses = IsRare(settings, reward)
            ? 0
            : Math.Min(GetMisses(id) + 1, settings.GuaranteeAfter - 1);

        ProjectPropertiesManager.Instance.SetLong(KeyPrefix + id, misses, save, ignoreSaveCooldown: true);
    }

    /// <summary>
    /// Гарант сейчас действует: включён, и среди доступных наград есть и редкие, и обычные.
    /// </summary>
    /// <remarks>
    /// Без редких гарантировать нечего — например, игрок собрал с колеса все редкие предметы.
    /// Без обычных редкая выпадает и так, и обещать её «через десять прокруток» значило бы врать.
    /// </remarks>
    private static bool IsActive(IReadOnlyList<WeightedRewardEntry> rewards, Predicate<RewardBase> filter,
        RewardPitySettings settings)
    {
        if (settings == null || !settings.Enabled || rewards == null)
            return false;

        bool hasRare = false;
        bool hasCommon = false;

        foreach (WeightedRewardEntry entry in rewards)
        {
            if (!IsSelectable(entry, filter))
                continue;

            if (IsRare(settings, entry.Item))
                hasRare = true;
            else
                hasCommon = true;

            if (hasRare && hasCommon)
                return true;
        }

        return false;
    }

    private static bool IsSelectable(WeightedRewardEntry entry, Predicate<RewardBase> filter)
    {
        return entry?.Item != null && entry.Weight > 0 && (filter == null || filter(entry.Item));
    }

    private static double GetWeight(WeightedRewardEntry entry, Predicate<RewardBase> filter,
        RewardPitySettings settings, bool active, bool guaranteed, double boost)
    {
        if (!IsSelectable(entry, filter))
            return 0d;

        if (!active)
            return entry.Weight;

        if (IsRare(settings, entry.Item))
            return entry.Weight * boost;

        return guaranteed ? 0d : entry.Weight;
    }
}
