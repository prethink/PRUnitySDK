# GameDataStorage

Папка содержит контракты сохранения PRUnitySDK, реализации storage и общие адаптеры
для данных внутри `ProjectData`.

## ProjectDataMap

`ProjectDataMap<TKey, TValue>` предоставляет единый набор операций над выбранным
словарём текущего `ProjectData`:

- `TryGetValue` — чтение с различением отсутствующего ключа;
- `GetValue` — чтение с fallback без изменения данных;
- `GetOrCreateValue` — совместимое создание значения по умолчанию;
- `SetValue` — запись с информацией о предыдущем значении;
- `TryRemoveValue` — удаление с возвратом удалённого значения.

```csharp
var resources = new ProjectDataMap<string, long>(
    () => GameManager.Instance.GetProjectData(),
    data => data.Resources ??= new Dictionary<string, long>());

ValueChange<long> change = resources.SetValue("Coin", 100);
```

`ValueChange<T>` сообщает, существовал ли ключ, предыдущее и текущее значения, а также
был ли словарь фактически изменён.

`ProjectDataMap` намеренно не вызывает `SaveProjectData()` и не публикует события.
Этими решениями владеет доменный фасад: например, `ResourceManager` сохраняет данные и
публикует `ResourceValueChangeEventArgs`, а `ProjectPropertiesManager` использует свои
типизированные события.

Доступ к map требует уже загруженного `ProjectData`. Вызывающий runtime-сервис должен
ориентироваться на `GameManager.ReadySignal`.

### Кто использует

| Сервис | Словарь в `ProjectData` | Что решает сам |
| --- | --- | --- |
| [ResourceManager](../Items/Resources/README.md) | `Resources` | сохранение, `IResourceValueChangedEvent` |
| [ProjectPropertiesManager](../@Managers/ProjectPropertiesManager/README.md) | словари `ProjectProperties` по типам | сохранение, типизированные события свойств |
| [TimeLimitedRewardService](../Reward/README.md#ограниченные-по-времени-награды) | `TimeLimitedRewards` | сохранение, события выдачи и истечения |

Заводить отдельный словарь стоит, когда данные образуют самостоятельный домен: их нужно
перечислять, чистить целиком или обрабатывать по своим правилам. Разовые значения без
такой потребности достаточно хранить в `ProjectProperties`.

## PRGameStorageService

`PRGameStorageService` предоставляет именованные категории типизированных значений,
которые хранятся через `ProjectPropertiesManager`. Категория добавляется к ключу, чтобы
значения разных storage не пересекались.

Это фасад над произвольными свойствами, тогда как `ProjectDataMap` — низкоуровневый
адаптер для словарей конкретных доменных данных.
