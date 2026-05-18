using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static T GetNext<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // »справл€ем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return list[currentIndex];
    }

    public static T GetPrevious<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // ƒвигаемс€ назад и корректируем индекс, чтобы он был в пределах списка
        currentIndex = (currentIndex - 1 + list.Count) % list.Count;

        return list[currentIndex];
    }

    public static T GetByStep<T>(this List<T> list, ref int currentIndex, int step)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        //  орректируем индекс с шагом и wrap-around
        currentIndex = (currentIndex + step) % list.Count;

        // ≈сли currentIndex стал отрицательным, поднимаем его в диапазон [0, Count-1]
        if (currentIndex < 0)
            currentIndex += list.Count;

        return list[currentIndex];
    }

    public static T GetWithUpdateIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        var previousIndex = currentIndex;

        // »справл€ем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return list[previousIndex];
    }

    public static int GetNextIndex<T>(this List<T> list, ref int currentIndex)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The list is empty.");

        // »справл€ем индекс, если он выходит за пределы списка
        currentIndex = (currentIndex + 1) % list.Count;

        return currentIndex;
    }
}