using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Хранит подписчиков одного event-интерфейса и создаёт новый snapshot только после
/// изменения состава подписок.
/// </summary>
/// <remarks>
/// Членство держит множество, порядок доставки - список. Без множества проверка дубликата
/// при подписке и поиск при отписке шли бы по всему списку, а у интерфейсов, которые
/// реализует каждый <see cref="PRMonoBehaviour"/>, в нём тысячи объектов: загрузка и
/// выгрузка сцены росли бы квадратично.
/// <para>
/// Из списка отписанные убираются не сразу, а одним проходом при пересборке snapshot:
/// объекты уходят пачками, и вычёркивать каждый отдельно дороже. Уничтоженные Unity-объекты
/// ищутся только тогда, когда публикация на них наткнулась - проверка «уничтожен ли объект»
/// идёт через нативный код, и полный проход на каждой публикации стоил бы тысячи вызовов.
/// </para>
/// </remarks>
internal sealed class SubscribersList<TSubscriber>
    where TSubscriber : class
{
    /// <summary>
    /// Подписчики в порядке подписки. Может ещё держать отписанных до пересборки snapshot.
    /// </summary>
    private readonly List<TSubscriber> subscribers = new();

    /// <summary>
    /// Кто подписан сейчас.
    /// </summary>
    private readonly HashSet<TSubscriber> registered = new(ReferenceComparer.Instance);

    /// <summary>
    /// Отписанные, которые ещё стоят в <see cref="subscribers"/>.
    /// </summary>
    private readonly HashSet<TSubscriber> pendingRemoval = new(ReferenceComparer.Instance);

    /// <summary>
    /// Массив, используемый публикациями до следующего изменения списка.
    /// </summary>
    private TSubscriber[] snapshot = Array.Empty<TSubscriber>();

    /// <summary>
    /// Указывает, что snapshot необходимо перестроить.
    /// </summary>
    private bool snapshotDirty = true;

    /// <summary>
    /// Сколько подписчиков числится. Уничтоженные Unity-объекты, которых ещё не встретила
    /// публикация, тоже считаются: точный счёт - <see cref="CountAlive"/>.
    /// </summary>
    public int Count => registered.Count;

    /// <summary>
    /// Добавляет подписчика, если этот экземпляр ещё не зарегистрирован.
    /// </summary>
    public bool Add(TSubscriber subscriber)
    {
        if (IsDead(subscriber) || !registered.Add(subscriber))
            return false;

        // Отписали и подписали заново до пересборки: запись ещё в списке, на своём месте.
        if (!pendingRemoval.Remove(subscriber))
            subscribers.Add(subscriber);

        snapshotDirty = true;
        return true;
    }

    /// <summary>
    /// Удаляет конкретный экземпляр подписчика.
    /// </summary>
    public bool Remove(TSubscriber subscriber)
    {
        if (ReferenceEquals(subscriber, null) || !registered.Remove(subscriber))
            return false;

        pendingRemoval.Add(subscriber);
        snapshotDirty = true;
        return true;
    }

    /// <summary>
    /// Возвращает стабильный snapshot для текущей публикации.
    /// </summary>
    public TSubscriber[] GetSnapshot()
    {
        if (!snapshotDirty)
            return snapshot;

        Compact();
        snapshot = subscribers.ToArray();
        snapshotDirty = false;
        return snapshot;
    }

    /// <summary>
    /// Убирает уничтоженные Unity-объекты. Зовётся, когда публикация на них наткнулась.
    /// </summary>
    public void RemoveDeadSubscribers()
    {
        for (int i = subscribers.Count - 1; i >= 0; i--)
        {
            TSubscriber subscriber = subscribers[i];

            if (!IsDead(subscriber))
                continue;

            registered.Remove(subscriber);
            pendingRemoval.Add(subscriber);
            snapshotDirty = true;
        }
    }

    /// <summary>
    /// Точное число живых подписчиков. Проходит весь список - не для частых вызовов.
    /// </summary>
    public int CountAlive()
    {
        RemoveDeadSubscribers();
        return registered.Count;
    }

    /// <summary>
    /// Вычёркивает отписанных из списка одним проходом, сохраняя порядок остальных.
    /// </summary>
    private void Compact()
    {
        if (pendingRemoval.Count == 0)
            return;

        subscribers.RemoveAll(pendingRemoval.Contains);
        pendingRemoval.Clear();
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

    /// <summary>
    /// Сравнение по ссылке, как и прежний поиск через <see cref="object.ReferenceEquals"/>.
    /// </summary>
    /// <remarks>
    /// Сравнение Unity по умолчанию считает уничтоженный объект равным null, а хеш берёт
    /// из идентификатора экземпляра - для учёта подписок нужна именно сама ссылка.
    /// </remarks>
    private sealed class ReferenceComparer : IEqualityComparer<TSubscriber>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(TSubscriber left, TSubscriber right) => ReferenceEquals(left, right);

        public int GetHashCode(TSubscriber subscriber) => RuntimeHelpers.GetHashCode(subscriber);
    }
}
