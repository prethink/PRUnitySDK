using System;
using System.Collections.Generic;

/// <summary>
/// Находит и кэширует event-интерфейсы, реализованные подписчиком.
/// </summary>
internal static class EventBusHelper
{
    /// <summary>
    /// Объект синхронизации кэша типов.
    /// </summary>
    private static readonly object cacheLock = new();

    /// <summary>
    /// Event-интерфейсы, сгруппированные по конкретному типу подписчика.
    /// </summary>
    private static readonly Dictionary<Type, Type[]> cachedSubscriberTypes = new();

    /// <summary>
    /// Возвращает все интерфейсы типа, наследующие <see cref="IGlobalSubscriber"/>.
    /// </summary>
    public static Type[] GetSubscriberTypes(IGlobalSubscriber globalSubscriber)
    {
        Type subscriberType = globalSubscriber.GetType();

        lock (cacheLock)
        {
            if (cachedSubscriberTypes.TryGetValue(subscriberType, out Type[] cachedTypes))
                return cachedTypes;

            Type[] interfaces = subscriberType.GetInterfaces();
            var subscriberTypes = new List<Type>(interfaces.Length);

            foreach (Type interfaceType in interfaces)
            {
                if (interfaceType == typeof(IGlobalSubscriber))
                    continue;

                if (typeof(IGlobalSubscriber).IsAssignableFrom(interfaceType))
                    subscriberTypes.Add(interfaceType);
            }

            Type[] result = subscriberTypes.ToArray();
            cachedSubscriberTypes[subscriberType] = result;
            return result;
        }
    }
}
