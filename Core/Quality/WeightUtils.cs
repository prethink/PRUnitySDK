using System;
using System.Collections.Generic;

public static class WeightUtils
{
    public static T GetRandomWeight<T>(List<WeightItem<T>> weightItems)
    {
        // Рассчитываем общий вес всех элементов
        float totalWeight = 0f;
        foreach (var item in weightItems)
        {
            totalWeight += item.Weight;
        }

        // Генерируем случайное значение от 0 до totalWeight
        float rndWeightValue = UnityEngine.Random.Range(0f, totalWeight);
        //Debug.Log($"Random weight value: {rndWeightValue}");

        // Ищем элемент, который соответствует случайному значению
        float accumulatedWeight = 0f;
        foreach (var item in weightItems)
        {
            accumulatedWeight += item.Weight;
            if (rndWeightValue <= accumulatedWeight)
            {
                return item.Item; // Возвращаем качество, которое соответствует выбранному весу
            }
        }

        // На случай, если все пошло не так, возвращаем первый элемент (по умолчанию)
        return weightItems[0].Item;
    }

    public static int GetRandomWeightIndex<T>(List<WeightItem<T>> weightItems)
    {
        if (weightItems == null || weightItems.Count == 0)
            throw new ArgumentException("Список weightItems не должен быть пустым.", nameof(weightItems));

        float totalWeight = 0f;
        foreach (var item in weightItems)
        {
            totalWeight += item.Weight;
        }

        float rndWeightValue = UnityEngine.Random.Range(0f, totalWeight);

        float accumulatedWeight = 0f;
        for (int i = 0; i < weightItems.Count; i++)
        {
            accumulatedWeight += weightItems[i].Weight;
            if (rndWeightValue <= accumulatedWeight)
            {
                return i;
            }
        }

        return 0; // На случай, если ничего не выбралось (маловероятно)
    }

}
