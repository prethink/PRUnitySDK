using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Шина событий.
/// </summary>
public static class EventBus
{
    #region Поля и свойства

    /// <summary>
    /// Подписчики событий.
    /// </summary>
    private static Dictionary<Type, SubscribersList<IGlobalSubscriber>> s_Subscribers = new Dictionary<Type, SubscribersList<IGlobalSubscriber>>();

    #endregion

    #region Методы

    /// <summary>
    /// Подписаться.
    /// </summary>
    /// <param name="subscriber">Подписчик.</param>
    public static void Subscribe(IGlobalSubscriber subscriber)
    {
        List<Type> subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);
        foreach (Type type in subscriberTypes)
        {
            if (!s_Subscribers.ContainsKey(type))
                s_Subscribers[type] = new SubscribersList<IGlobalSubscriber>();

            s_Subscribers[type].Add(subscriber);
        }
    }

    /// <summary>
    /// Отписаться.
    /// </summary>
    /// <param name="subscriber">Подписчик.</param>
    public static void Unsubscribe(IGlobalSubscriber subscriber)
    {
        List<Type> subscriberTypes = EventBusHelper.GetSubscriberTypes(subscriber);
        foreach (Type type in subscriberTypes)
        {
            if (s_Subscribers.ContainsKey(type))
                s_Subscribers[type].Remove(subscriber);
        }
    }

    /// <summary>
    /// Вызвать событие.
    /// </summary>
    /// <typeparam name="TSubscriber">Тип подписчика.</typeparam>
    /// <param name="action">Метод вызова.</param>
    public static void RaiseEvent<TSubscriber>(Action<TSubscriber> action) where TSubscriber : class, IGlobalSubscriber
    {
        if (!s_Subscribers.ContainsKey(typeof(TSubscriber)))
            return;

        SubscribersList<IGlobalSubscriber> subscribers = s_Subscribers[typeof(TSubscriber)];
        subscribers.Executing = true;
        foreach (IGlobalSubscriber subscriber in subscribers.List.ToList())
        {
            try
            {
                action.Invoke(subscriber as TSubscriber);
            }
            catch (Exception e)
            {
                Debug.LogError($"{subscribers.GetType()} - {e}");
            }
        }
        subscribers.Executing = false;
        subscribers.Cleanup();
    }

    #endregion
}
