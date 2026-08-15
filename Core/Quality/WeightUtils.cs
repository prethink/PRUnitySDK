using System;
using System.Collections.Generic;

/// <summary>
/// Выполняет случайный выбор элементов пропорционально их целочисленным весам.
/// </summary>
public static class WeightUtils
{
    /// <summary>
    /// Пытается выбрать элемент с положительным весом.
    /// </summary>
    public static bool TryGetRandom<T>(
        IReadOnlyList<WeightItem<T>> weightItems,
        out T item,
        Predicate<T> predicate = null)
    {
        if (TryGetRandomIndex(weightItems, out int index, predicate))
        {
            item = weightItems[index].Item;
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Пытается получить индекс элемента с положительным весом.
    /// Нулевые веса и пустые записи не участвуют в выборе.
    /// </summary>
    public static bool TryGetRandomIndex<T>(
        IReadOnlyList<WeightItem<T>> weightItems,
        out int index,
        Predicate<T> predicate = null)
    {
        index = -1;
        if (!TryGetTotalWeight(weightItems, predicate, out ulong totalWeight))
            return false;

        ulong randomWeight = NextUInt64(totalWeight);
        ulong accumulatedWeight = 0;

        for (int itemIndex = 0; itemIndex < weightItems.Count; itemIndex++)
        {
            WeightItem<T> weightedItem = weightItems[itemIndex];
            if (!IsSelectable(weightedItem, predicate))
                continue;

            accumulatedWeight += weightedItem.Weight;
            if (randomWeight < accumulatedWeight)
            {
                index = itemIndex;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Выбирает элемент либо выбрасывает исключение, если выбрать нечего.
    /// </summary>
    public static T GetRandomWeight<T>(IReadOnlyList<WeightItem<T>> weightItems)
    {
        if (TryGetRandom(weightItems, out T item))
            return item;

        throw new InvalidOperationException(
            "Weighted collection must contain a valid item with a positive weight and must not overflow UInt64.");
    }

    /// <summary>
    /// Выбирает индекс либо выбрасывает исключение, если выбрать нечего.
    /// </summary>
    public static int GetRandomWeightIndex<T>(IReadOnlyList<WeightItem<T>> weightItems)
    {
        if (TryGetRandomIndex(weightItems, out int index))
            return index;

        throw new InvalidOperationException(
            "Weighted collection must contain a valid item with a positive weight and must not overflow UInt64.");
    }

    /// <summary>
    /// Возвращает вероятность веса относительно общей суммы в диапазоне от 0 до 1.
    /// </summary>
    public static double GetProbability(ulong weight, ulong totalWeight)
    {
        return totalWeight == 0 ? 0d : (double)weight / totalWeight;
    }

    private static bool TryGetTotalWeight<T>(
        IReadOnlyList<WeightItem<T>> weightItems,
        Predicate<T> predicate,
        out ulong totalWeight)
    {
        totalWeight = 0;
        if (weightItems == null || weightItems.Count == 0)
            return false;

        for (int index = 0; index < weightItems.Count; index++)
        {
            WeightItem<T> weightedItem = weightItems[index];
            if (!IsSelectable(weightedItem, predicate))
                continue;

            if (ulong.MaxValue - totalWeight < weightedItem.Weight)
                return false;

            totalWeight += weightedItem.Weight;
        }

        return totalWeight > 0;
    }

    private static bool IsSelectable<T>(WeightItem<T> weightedItem, Predicate<T> predicate)
    {
        return weightedItem != null &&
               weightedItem.Weight > 0 &&
               (predicate == null || predicate(weightedItem.Item));
    }

    private static ulong NextUInt64(ulong exclusiveMaximum)
    {
        if (exclusiveMaximum == 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

        // Отбрасываем неполный диапазон, чтобы операция modulo не смещала вероятность.
        ulong rejectionThreshold = unchecked(0UL - exclusiveMaximum) % exclusiveMaximum;
        ulong value;

        do
        {
            value = ((ulong)UnityEngine.Random.Range(0, 1 << 16) << 48) |
                    ((ulong)UnityEngine.Random.Range(0, 1 << 16) << 32) |
                    ((ulong)UnityEngine.Random.Range(0, 1 << 16) << 16) |
                    (uint)UnityEngine.Random.Range(0, 1 << 16);
        }
        while (value < rejectionThreshold);

        return value % exclusiveMaximum;
    }
}
