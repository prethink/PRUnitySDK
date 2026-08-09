using System;
using System.Collections.Generic;

/// <summary>
/// Хранит подписчиков одного event-интерфейса и создаёт новый snapshot только после
/// изменения состава подписок.
/// </summary>
internal sealed class SubscribersList<TSubscriber>
    where TSubscriber : class
{
    /// <summary>
    /// Текущий изменяемый список подписчиков.
    /// </summary>
    private readonly List<TSubscriber> subscribers = new();

    /// <summary>
    /// Массив, используемый публикациями до следующего изменения списка.
    /// </summary>
    private TSubscriber[] snapshot = Array.Empty<TSubscriber>();

    /// <summary>
    /// Указывает, что snapshot необходимо перестроить.
    /// </summary>
    private bool snapshotDirty = true;

    /// <summary>
    /// Возвращает количество живых подписчиков.
    /// </summary>
    public int Count
    {
        get
        {
            RemoveDeadSubscribers();
            return subscribers.Count;
        }
    }

    /// <summary>
    /// Добавляет подписчика, если этот экземпляр ещё не зарегистрирован.
    /// </summary>
    public bool Add(TSubscriber subscriber)
    {
        if (IsDead(subscriber))
            return false;

        for (int i = 0; i < subscribers.Count; i++)
        {
            if (ReferenceEquals(subscribers[i], subscriber))
                return false;
        }

        subscribers.Add(subscriber);
        snapshotDirty = true;
        return true;
    }

    /// <summary>
    /// Удаляет конкретный экземпляр подписчика.
    /// </summary>
    public bool Remove(TSubscriber subscriber)
    {
        if (ReferenceEquals(subscriber, null))
            return false;

        for (int i = 0; i < subscribers.Count; i++)
        {
            if (!ReferenceEquals(subscribers[i], subscriber))
                continue;

            subscribers.RemoveAt(i);
            snapshotDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Возвращает стабильный snapshot для текущей публикации.
    /// </summary>
    public TSubscriber[] GetSnapshot()
    {
        RemoveDeadSubscribers();

        if (!snapshotDirty)
            return snapshot;

        snapshot = subscribers.ToArray();
        snapshotDirty = false;
        return snapshot;
    }

    /// <summary>
    /// Удаляет обычные null-ссылки и уничтоженные Unity-объекты.
    /// </summary>
    private void RemoveDeadSubscribers()
    {
        bool removed = false;

        for (int i = subscribers.Count - 1; i >= 0; i--)
        {
            if (!IsDead(subscribers[i]))
                continue;

            subscribers.RemoveAt(i);
            removed = true;
        }

        if (removed)
            snapshotDirty = true;
    }

    /// <summary>
    /// Проверяет обычный null и специальное состояние уничтоженного Unity-объекта.
    /// </summary>
    private static bool IsDead(TSubscriber subscriber)
    {
        if (ReferenceEquals(subscriber, null))
            return true;

        return subscriber is UnityEngine.Object unityObject && unityObject == null;
    }
}
