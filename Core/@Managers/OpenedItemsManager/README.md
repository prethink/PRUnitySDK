# OpenedItemsManager

`OpenedItemsManager` ведёт открытые игроком предметы в `GameManager.GetProjectData().OpenedItems`.
Менеджер — обычный singleton, который контейнер публикует как `PRUnitySDK.Managers.OpenedItems`.

Используйте его только после `GameManager.ReadySignal`, когда `ProjectData` уже загружен.

## Три разных вопроса

Менеджер отвечает на три вопроса, и путать их нельзя:

| Вопрос | Метод | Меняется тратой |
| --- | --- | --- |
| Открывался ли предмет когда-либо | `IsOpenedItem` | нет |
| Сколько его сейчас | `GetCount` | да |
| Что именно открыто — шапки или брейнроты | `GetOpenedByCategory` | нет |

Разделение нужно расходуемым предметам. Потратив последний ключ, игрок не перестаёт знать
о ключах; купленный скин не должен снова оказаться в продаже. Поэтому `ItemStack` хранит
и текущее количество (`Count`), и всё полученное за время игры (`TotalOpened`), а запись
не удаляется при нулевом остатке.

```csharp
bool opened = PRUnitySDK.Managers.OpenedItems.IsOpenedItem(itemDefinition);
int count = PRUnitySDK.Managers.OpenedItems.GetCount(itemDefinition);
bool enough = PRUnitySDK.Managers.OpenedItems.HasCount(itemDefinition, 3);
```

## Вид предмета и источник

У записи две независимые пометки, и обе полезны:

| Поле | Отвечает на вопрос | Пример |
| --- | --- | --- |
| `Category` | что это за предмет | `HatDefinition`, `BrainrotDefinition` |
| `Created` | откуда он взялся | `ShopService`, `LootContainer` |

Одно другому не мешает: шапка остаётся шапкой, придя из награды.

```csharp
// все открытые брейнроты, не смешивая с шапками
IEnumerable<string> ids = PRUnitySDK.Managers.OpenedItems.GetOpenedIds(nameof(BrainrotDefinition));

// открыт ли конкретный, с проверкой вида
bool unlocked = PRUnitySDK.Managers.OpenedItems.IsOpenedInCategory<BrainrotDefinition>(id);

// открыт ли вообще, без указания вида
bool owned = PRUnitySDK.Managers.OpenedItems.IsOpenedItem(id);

// пришёл ли именно из магазина
bool bought = PRUnitySDK.Managers.OpenedItems.IsOpenedItem(nameof(ShopService), itemDefinition.Id);
```

Вид определяется по типу определения в момент открытия и записывается в сохранение.
Вывести его позже нельзя: в данных от предмета остаётся только идентификатор,
поэтому у записей из старых сохранений вид пустой — он проставится при следующей выдаче
того же предмета.

## Открыть или добавить

Два метода под два разных вида предметов:

| Метод | Для чего | Повторный вызов |
| --- | --- | --- |
| `Open` | того, что просто есть или нет: брейнрот в коллекции, купленный скин, открытая способность | ничего не меняет |
| `Add` | расходуемого: ключей, билетов, патронов | увеличивает количество |

```csharp
// брейнрот подобран впервые - покажем поздравление
if (PRUnitySDK.Managers.OpenedItems.Open(typeof(BrainrotHolder), brainrotDefinition))
    ShowUnlockPopup(brainrotDefinition);

// три ключа в награду
PRUnitySDK.Managers.OpenedItems.Add(typeof(RewardSystem), keyDefinition, count: 3);
```

`Open` возвращает «это впервые?» — по нему показывают поздравление или подсвечивают
новинку. Для косметики он и нужен: `Add` при повторной покупке превратил бы один скин
в «две штуки». Так работает `ShopService.GrantOwnership`.

Если стека с таким `Id` нет, менеджер создаёт его и добавляет в `ProjectData.OpenedItems`,
затем увеличивает и текущее количество, и общее полученное. При `requiredSave: true`
вызывается `GameManager.SaveProjectData()`.

## Трата

```csharp
bool spent = PRUnitySDK.Managers.OpenedItems.TryRemoveItem(keyDefinition, count: 1);
```

Возвращает `false`, если предметов меньше, чем нужно, и ничего не меняет. Уходя в ноль,
запись остаётся — иначе предмет снова считался бы неоткрытым.

## Совместимость сохранений

`TotalOpened` появился позже `Count`, и в старых сохранениях его нет. Поэтому предмет
считается открытым, если ненулевое любое из двух чисел: у прежних записей единственный
след открытия — количество.

## Кто пользуется

- `RewardItemGrantHandler` — выдача `RewardItem`
- `LootContainer` — предметы из контейнеров
- `ShopService` — покупки; владение у магазина общее с наградами, своего списка он не ведёт
- `BrainrotUnlockService` — открытые брейнроты под видом `BrainrotDefinition`

## Что хранится

`ItemStack` держит идентификатор строкой, а не ссылку на определение. В сохранение
и раньше попадал только идентификатор, а при загрузке на месте предмета оказывалась
пустая заглушка — строка честнее показывает, что там на самом деле. Определение по нему
берут из каталога, когда оно нужно.

Идентификатор — произвольная строка, а не обязательно GUID. Предметы каталогов получают
сгенерированный GUID, но у ресурса это ключ его `Enumeration`:

```csharp
public class YandexCurrencyDefinition : ResourceItemDefinitionBase
{
    public override string Id => ResourceEnumerationProvider.Yan.Value;   // "Yan"
}
```

Поэтому менеджер сравнивает идентификаторы как строки и нигде их не разбирает. Не
добавляйте проверок вида `Guid.TryParse(id)`: они молча отсекут всё, кроме предметов
каталогов.
