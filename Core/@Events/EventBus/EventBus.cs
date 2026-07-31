using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Шина событий с потокобезопасностью и защитой от дублей.
/// </summary>
public static class EventBus
{
    private static readonly object _lock = new object();
    private static Dictionary<Type, SubscribersList<IGlobalSubscriber>> _subscribers =
        new Dictionary<Type, SubscribersList<IGlobalSubscriber>>();

    /// <summary>
    /// Подписаться на события.
    /// </summary>
    public static void Subscribe(IGlobalSubscriber subscriber)
    {
        if (subscriber == null)
        {
            Debug.LogWarning("Попытка подписать null подписчика на EventBus");
            return;
        }

        lock (_lock)
        {
            List<Type> subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);

            foreach (Type type in subscriberTypes)
            {
                if (!_subscribers.ContainsKey(type))
                    _subscribers[type] = new SubscribersList<IGlobalSubscriber>();

                // Проверяем чтобы не было дубликатов
                var list = _subscribers[type];
                if (!list.List.Contains(subscriber))
                {
                    list.Add(subscriber);
                    //Debug.Log($"[EventBus] Подписан {subscriber.GetType().Name} на {type.Name}");
                }
                else
                {
                    Debug.LogWarning($"[EventBus] {subscriber.GetType().Name} уже подписан на {type.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Отписаться от событий.
    /// </summary>
    public static void Unsubscribe(IGlobalSubscriber subscriber)
    {
        if (subscriber == null)
            return;

        lock (_lock)
        {
            List<Type> subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);

            foreach (Type type in subscriberTypes)
            {
                if (_subscribers.ContainsKey(type))
                {
                    _subscribers[type].Remove(subscriber);
                    //Debug.Log($"[EventBus] Отписан {subscriber.GetType().Name} от {type.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Вызвать событие для всех подписчиков.
    /// </summary>
    public static void RaiseEvent<TSubscriber>(Action<TSubscriber> action)
        where TSubscriber : class, IGlobalSubscriber
    {
        if (action == null)
        {
            Debug.LogError("[EventBus] Action равна null в RaiseEvent");
            return;
        }

        SubscribersList<IGlobalSubscriber> subscribers = null;

        lock (_lock)
        {
            if (!_subscribers.ContainsKey(typeof(TSubscriber)))
                return;

            subscribers = _subscribers[typeof(TSubscriber)];
            subscribers.Executing = true;
        }

        // Выполняем вне lock'а чтобы избежать deadlock
        try
        {
            foreach (IGlobalSubscriber subscriber in subscribers.List.ToList())
            {
                try
                {
                    action.Invoke(subscriber as TSubscriber);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Ошибка при вызове события для {subscriber?.GetType().Name}: {e}");
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                subscribers.Executing = false;
                subscribers.Cleanup();
            }
        }
    }

    /// <summary>
    /// Возвращает количество подписчиков на тип события.
    /// </summary>
    public static int GetSubscriberCount<TSubscriber>()
        where TSubscriber : class, IGlobalSubscriber
    {
        lock (_lock)
        {
            if (!_subscribers.ContainsKey(typeof(TSubscriber)))
                return 0;

            return _subscribers[typeof(TSubscriber)].List.Count;
        }
    }

    /// <summary>
    /// Очищает все подписки (используй осторожно!).
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _subscribers.Clear();
            Debug.LogWarning("[EventBus] Все подписки очищены");
        }
    }
}