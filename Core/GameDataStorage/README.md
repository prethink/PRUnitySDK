# GameDataStorage

Папка содержит контракты сохранения PRUnitySDK, реализации storage и общие адаптеры
для данных внутри `ProjectData`.

## Save metadata

`PRSaveData.SaveDate` хранит дату создания save, а `UpdateDate` обновляется непосредственно
перед каждой записью стандартными `PlayerPrefsSaveLoadManager` и `YandexGameDataStorager`.
Обе даты берутся из `PRUnitySDK.ServerTime`, поэтому используют единый настроенный источник времени.
Поле добавлено обратно совместимо: для старого save без `UpdateDate` стандартные storage
используют `SaveDate` как fallback до следующей записи.

Storage может дополнительно реализовать `IGameDataStorageSaveInfo`, не расширяя обязательный
контракт `IGameDataStorage`. `GameManager` использует этот интерфейс, чтобы восстановить даты
создания и последнего обновления после перезапуска. Собственная реализация storage может вернуть `null`,
если метаданные недоступны.

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
