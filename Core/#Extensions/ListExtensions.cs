using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static T GetNext<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        currentIndex = (currentIndex + 1) % list.Count;

        return list[currentIndex];
    }

    public static T GetPrevious<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        currentIndex = (currentIndex - 1 + list.Count) % list.Count;

        return list[currentIndex];
    }

    public static T GetByStep<T>(this List<T> list, ref int currentIndex, int step)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        currentIndex = (currentIndex + step) % list.Count;

        if (currentIndex < 0)
            currentIndex += list.Count;

        return list[currentIndex];
    }

    public static T GetWithUpdateIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        currentIndex %= list.Count;
        if (currentIndex < 0)
            currentIndex += list.Count;

        var previousIndex = currentIndex;

        currentIndex = (currentIndex + 1) % list.Count;

        return list[previousIndex];
    }

    public static int GetNextIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        currentIndex = (currentIndex + 1) % list.Count;

        return currentIndex;
    }

    /// <summary>
    /// </summary>
    public static void AddOrReplace<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, T item)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var key = keySelector(item);

        for (int i = 0; i < list.Count; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(keySelector(list[i]), key))
            {
                list[i] = item;
                return;
            }
        }

        list.Add(item);
    }
}
