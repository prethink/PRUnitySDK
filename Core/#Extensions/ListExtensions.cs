using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static T GetNext<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // Исправляем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return list[currentIndex];
    }

    public static T GetPrevious<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // Двигаемся назад и корректируем индекс, чтобы он был в пределах списка
        currentIndex = (currentIndex - 1 + list.Count) % list.Count;

        return list[currentIndex];
    }

    public static T GetByStep<T>(this List<T> list, ref int currentIndex, int step)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // Корректируем индекс с шагом и wrap-around
        currentIndex = (currentIndex + step) % list.Count;

        // Если currentIndex стал отрицательным, поднимаем его в диапазон [0, Count-1]
        if (currentIndex < 0)
            currentIndex += list.Count;

        return list[currentIndex];
    }

    public static T GetWithUpdateIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        var previousIndex = currentIndex;

        // Исправляем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return list[previousIndex];
    }

    public static int GetNextIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // Исправляем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return currentIndex;
    }

    /// <summary>
    /// Добавляет элемент или заменяет существующий по ключу.
    /// </summary>
    public static void AddOrReplace<T, TKey>(this IList<T> list,Func<T, TKey> keySelector, T item)
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