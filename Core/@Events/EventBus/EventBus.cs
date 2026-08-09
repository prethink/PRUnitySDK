using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Глобальная типизированная шина событий на основе интерфейсов подписчиков.
/// </summary>
public static class EventBus
{
    /// <summary>
    /// Объект синхронизации доступа к реестру подписчиков.
    /// </summary>
    private static readonly object subscribersLock = new();

    /// <summary>
    /// Списки подписчиков, сгруппированные по event-интерфейсам.
    /// </summary>
    private static readonly Dictionary<Type, SubscribersList<IGlobalSubscriber>> subscribers = new();

    /// <summary>
    /// Регистрирует объект во всех реализованных им event-интерфейсах.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если подписчик был добавлен хотя бы в один список.
    /// </returns>
    public static bool Subscribe(IGlobalSubscriber subscriber)
    {
        if (IsDead(subscriber))
        {
            Debug.LogWarning("[EventBus] Невозможно подписать null или уничтоженный Unity-объект.");
            return false;
        }

        Type[] subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);
        bool added = false;

        lock (subscribersLock)
        {
            foreach (Type subscriberType in subscriberTypes)
            {
                if (!subscribers.TryGetValue(subscriberType, out SubscribersList<IGlobalSubscriber> list))
                {
                    list = new SubscribersList<IGlobalSubscriber>();
                    subscribers.Add(subscriberType, list);
                }

                added |= list.Add(subscriber);
            }
        }

        return added;
    }

    /// <summary>
    /// Удаляет объект из всех реализованных им event-интерфейсов.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если подписчик был удалён хотя бы из одного списка.
    /// </returns>
    public static bool Unsubscribe(IGlobalSubscriber subscriber)
    {
        if (ReferenceEquals(subscriber, null))
            return false;

        Type[] subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);
        bool removed = false;

        lock (subscribersLock)
        {
            foreach (Type subscriberType in subscriberTypes)
            {
                if (!subscribers.TryGetValue(subscriberType, out SubscribersList<IGlobalSubscriber> list))
                    continue;

                removed |= list.Remove(subscriber);

                if (list.Count == 0)
                    subscribers.Remove(subscriberType);
            }
        }

        return removed;
    }

    /// <summary>
    /// Вызывает событие у всех подписчиков указанного event-интерфейса.
    /// </summary>
    public static void RaiseEvent<TSubscriber>(Action<TSubscriber> action)
        where TSubscriber : class, IGlobalSubscriber
    {
        if (action == null)
        {
            Debug.LogError("[EventBus] Невозможно вызвать событие с null action.");
            return;
        }

        IGlobalSubscriber[] snapshot;

        lock (subscribersLock)
        {
            if (!subscribers.TryGetValue(typeof(TSubscriber), out SubscribersList<IGlobalSubscriber> list))
                return;

            snapshot = list.GetSnapshot();

            if (snapshot.Length == 0)
            {
                subscribers.Remove(typeof(TSubscriber));
                return;
            }
        }

        foreach (IGlobalSubscriber subscriber in snapshot)
        {
            if (IsDead(subscriber) || subscriber is not TSubscriber typedSubscriber)
                continue;

            try
            {
                action.Invoke(typedSubscriber);
            }
            catch (Exception exception)
            {
                LogSubscriberException(subscriber, exception);
            }
        }
    }

    /// <summary>
    /// Возвращает количество живых подписчиков указанного event-интерфейса.
    /// </summary>
    public static int GetSubscriberCount<TSubscriber>()
        where TSubscriber : class, IGlobalSubscriber
    {
        lock (subscribersLock)
        {
            if (!subscribers.TryGetValue(typeof(TSubscriber), out SubscribersList<IGlobalSubscriber> list))
                return 0;

            int count = list.Count;

            if (count == 0)
                subscribers.Remove(typeof(TSubscriber));

            return count;
        }
    }

    /// <summary>
    /// Удаляет все глобальные подписки.
    /// </summary>
    public static void Clear()
    {
        lock (subscribersLock)
            subscribers.Clear();
    }

    /// <summary>
    /// Проверяет обычный null и специальное состояние уничтоженного Unity-объекта.
    /// </summary>
    private static bool IsDead(IGlobalSubscriber subscriber)
    {
        if (ReferenceEquals(subscriber, null))
            return true;

        return subscriber is UnityEngine.Object unityObject && unityObject == null;
    }

    /// <summary>
    /// Записывает исключение обработчика с Unity-контекстом, если он доступен.
    /// </summary>
    private static void LogSubscriberException(IGlobalSubscriber subscriber, Exception exception)
    {
        if (subscriber is UnityEngine.Object unityObject && unityObject != null)
        {
            Debug.LogException(exception, unityObject);
            return;
        }

        Debug.LogException(exception);
    }
}
