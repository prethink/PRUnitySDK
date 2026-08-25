using System;

/// <summary>
/// Временная награда выдана или продлена.
/// <para>
/// Отдельное событие, а не общее изменение DateTime-свойства: подписчик получает
/// только награды и уже готовый момент окончания, без фильтрации чужих дат по имени.
/// </para>
/// </summary>
public interface ITimeLimitedRewardChangedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после записи нового времени окончания.
    /// </summary>
    /// <param name="key">Ключ награды.</param>
    /// <param name="endTime">Новый момент окончания.</param>
    /// <param name="wasActive">Была ли награда активна до этой операции.
    /// False означает, что награда выдана заново, а не продлена.</param>
    void OnTimeLimitedRewardChanged(string key, DateTime endTime, bool wasActive);
}

/// <summary>
/// Временная награда истекла.
/// <para>
/// Публикуется при удалении истёкшей записи - до этого узнать об окончании было
/// нельзя: награда просто переставала считаться активной при следующей проверке.
/// </para>
/// </summary>
public interface ITimeLimitedRewardExpiredEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается для награды, срок которой вышел.
    /// </summary>
    /// <param name="key">Ключ награды.</param>
    /// <param name="endTime">Момент, когда действие закончилось.</param>
    void OnTimeLimitedRewardExpired(string key, DateTime endTime);
}
