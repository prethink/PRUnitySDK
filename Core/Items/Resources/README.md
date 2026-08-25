# ResourceManager

`ResourceManager` управляет числовыми игровыми ресурсами: валютой, материалами и
другими значениями `long`, идентифицированными через `Enumeration`.

Данные хранятся в существующем `ProjectData.Resources`, поэтому формат сохранения и
совместимость старых сейвов не изменились. Менеджер доступен через:

```csharp
ResourceManager resources = PRUnitySDK.Managers.Resource;
```

Обращаться к данным следует после `PRUnitySDK.Managers.Game.ReadySignal`.

## Чтение и изменение

```csharp
long coins = resources.GetResource(ResourceEnumerationProvider.Coin);

resources.SetOrUpdateResource(
    ResourceEnumerationProvider.Coin,
    100,
    requiredNotify: true,
    requiredSaveNow: true);

resources.AddResourceValue(
    ResourceEnumerationProvider.Coin,
    25,
    requiredNotify: true,
    requiredSaveNow: false);
```

`GetResource` не изменяет `ProjectData`. `TryGetResource` позволяет отличить
отсутствующий ключ от сохранённого нуля. `GetOrCreateResource` сохранён для обратной
совместимости и создаёт отсутствующий ключ со значением `0`.

Повторная установка того же значения не выполняет save и не публикует событие.

## ResourceItemDefinition

Основные операции имеют перегрузки с `ResourceItemDefinition`. Менеджер получает
runtime-тип из `CurrencyType`, поэтому вызывающему коду не нужно вручную обращаться к
`EnumerationReference`:

```csharp
[SerializeField] private ResourceItemDefinition coins;

long balance = resources.GetResource(coins);

resources.AddResourceValue(
    coins,
    25,
    requiredNotify: true,
    requiredSaveNow: true);

bool purchased = resources.TrySpendResource(
    coins,
    amount: 10,
    requiredNotify: true,
    requiredSaveNow: true);
```

Поддерживаются `TryGetResource`, `GetResource`, `GetOrCreateResource`,
`SetOrUpdateResource`, `AddResourceValue` и `TrySpendResource`. Если definition равен
`null` или его `CurrencyType` не настроен, операция безопасно завершается и пишет
warning; чтение возвращает fallback, а списание — `false`.

Для самостоятельной проверки definition доступен
`ResourceItemDefinition.TryGetResourceType(out Enumeration)`.

## Безопасное списание

`TrySpendResource` проверяет неотрицательность суммы и достаточность ресурса, затем
выполняет списание одной доменной операцией:

```csharp
bool purchased = resources.TrySpendResource(
    ResourceEnumerationProvider.Coin,
    amount: 50,
    requiredNotify: true,
    requiredSaveNow: true);
```

При неуспешной проверке данные, события и сохранение не изменяются.

`WalletService.Buy` использует этот метод. `WalletService.Add` передаёт параметры save и
notify именованно, чтобы они не смешивались в legacy API `ResourceManager`.

## Уведомления

При `requiredNotify: true` изменение публикуется в EventBus на двух уровнях — подписчик
выбирает удобный:

| Интерфейс | Метод | Когда использовать |
| --- | --- | --- |
| `IResourceValueChangedEvent` | `OnResourceValueChanged(ResourceValueChangeEventArgs)` | нужны значения: показать количество, посчитать прирост |
| `IResourceEvent` | `OnResourceUpdate(ResourceEventArgs)` | достаточно факта изменения: обновить экран, записать метрику |

Сначала уведомляются подписчики со значениями, затем общие. Приводить тип аргумента
вручную больше не нужно.

```csharp
public class CoinsView : MonoBehaviour, IResourceValueChangedEvent
{
    public void OnResourceValueChanged(ResourceValueChangeEventArgs args)
    {
        if (args.ResourceType != ResourceEnumerationProvider.Coins)
            return;

        label.text = args.CurrentValue.ToString();

        if (args.Delta.HasValue)
            PlayGainAnimation(args.Delta.Value);
    }
}
```

| Свойство | Назначение |
| --- | --- |
| `ResourceType` | изменённый ресурс |
| `PreviousValue` | значение до операции, осмысленно при `HasPreviousValue` |
| `CurrentValue` | значение после операции |
| `HasPreviousValue` | известно ли предыдущее значение |
| `Delta` | разница current и previous либо `null`, если сравнивать не с чем |
| `Value` | совместимый alias `CurrentValue` |

`Delta` намеренно nullable: ноль означает «значение не изменилось», а `null` — «предыдущее
значение неизвестно». Раньше оба случая давали ноль, и подписчик не мог их различить.

Сначала при необходимости выполняется `SaveProjectData()`, затем публикуется событие.

## Универсальное хранилище

Внутри менеджер использует `ProjectDataMap<string, long>`. Map отвечает только за
операции со словарём и возвращает `ValueChange<long>`; решение о сохранении и событии
остаётся в `ResourceManager`.

Подробности находятся в [GameDataStorage](../../GameDataStorage/README.md).

## Плавное изменение

`UpdateResourceValueSmooth` и `AddResourceValueSmooth` оставлены для совместимости, но
они изменяют фактические данные каждый кадр. Для UI рекомендуется сразу записывать
итоговое значение и визуально интерполировать `PreviousValue` → `CurrentValue` после
получения события.
