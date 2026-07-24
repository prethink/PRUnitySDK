using System;

/// <summary>
/// Представляет сигнал, который указывает на готовность операции или ресурса.
/// Позволяет подписаться на уведомления о готовности через callback'и.
/// </summary>
public interface IReadySignal
{
    /// <summary>
    /// Получает значение, указывающее, был ли сигнал помечен как готовый.
    /// </summary>
    /// <remarks>
    /// Когда это свойство истинно, любой новый подписчик будет вызван немедленно.
    /// </remarks>
    bool IsReady { get; }

    /// <summary>
    /// Подписывает callback на событие готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для вызова при готовности. Если уже готово, вызывается немедленно.</param>
    /// <returns>Возвращает этот экземпляр для цепочки методов.</returns>
    /// <remarks>
    /// Если <see cref="IsReady"/> уже true, callback вызывается синхронно перед возвратом.
    /// Если false, callback сохраняется и вызывается при вызове SetReady().
    /// Передача null безопасна и будет проигнорирована.
    /// </remarks>
    IReadySignal SubscribeOnReady(Action onReadyCallback);

    /// <summary>
    /// Отписывает callback от события готовности.
    /// </summary>
    /// <param name="onReadyCallback">Действие для удаления из списка подписок.</param>
    /// <remarks>
    /// Если callback не подписан, метод не имеет эффекта.
    /// </remarks>
    void UnSubscribe(Action onReadyCallback);
}