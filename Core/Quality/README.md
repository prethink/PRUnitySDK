# Quality

Система качества предметов, похожая на системы редкости из CS, Dota и TF2.

## Weighted random

`WeightUtils` выбирает элементы пропорционально `WeightItem<T>.Weight`.
Нулевые веса и пустые записи пропускаются.

Безопасный вариант не выбрасывает исключений:

```csharp
if (WeightUtils.TryGetRandom(items, out RewardDefinition reward))
{
    GiveReward(reward);
}
```

Можно получить индекс и дополнительно отфильтровать элементы без создания новой коллекции:

```csharp
bool selected = WeightUtils.TryGetRandomIndex(
    items,
    out int index,
    item => item != null);
```

Методы возвращают `false`, если коллекция пуста, все веса равны нулю либо сумма весов
превышает `ulong.MaxValue`. `GetRandomWeight` и `GetRandomWeightIndex` оставлены как
строгие варианты и в этих случаях выбрасывают `InvalidOperationException`.

Вероятность отдельного веса можно рассчитать через:

```csharp
double probability = WeightUtils.GetProbability(weight, totalWeight);
```
