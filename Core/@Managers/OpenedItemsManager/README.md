# OpenedItemsManager

`OpenedItemsManager` хранит количество открытых предметов в `GameManager.GetProjectData().OpenedItems`. Менеджер — обычный singleton, который контейнер публикует как `PRUnitySDK.Managers.OpenedItems`.

Используйте его только после `GameManager.ReadySignal`, когда `ProjectData` уже загружен.

## Проверка предмета

```csharp
bool opened = PRUnitySDK.Managers.OpenedItems.IsOpenedItem(itemDefinition);
bool openedBySystem = PRUnitySDK.Managers.OpenedItems.IsOpenedItem(
    typeof(RewardSystem),
    itemDefinition);
```

Перегрузки без `type` ищут любой `ItemStack` с указанным `Id`. Перегрузки с `type` дополнительно требуют совпадения `ItemStack.Created` со строкой типа (`type.ToString()`).

## Добавление

```csharp
bool added = PRUnitySDK.Managers.OpenedItems.AddOpenItem(
    typeof(RewardSystem),
    itemDefinition,
    count: 3,
    requiredSave: true);
```

Если stack с таким `Id` отсутствует, менеджер создаёт его через `ItemStack.CreateEmpty(type, selectableItem)`, добавляет в `ProjectData.OpenedItems`, затем увеличивает количество. При `requiredSave: true` вызывается `GameManager.SaveProjectData()`.

Fallback-обработчик `RewardItemGrantHandler` использует этот менеджер для выдачи `RewardItem`.

## Публичный API

| Метод | Назначение |
| --- | --- |
| `IsOpenedItem(IIdentifiable|string)` | проверить наличие предмета по `Id` без учёта создавшей системы |
| `IsOpenedItem(Type|string, IIdentifiable|string)` | проверить `Id` и значение `Created` |
| `AddOpenItem(Type|string, IIdentifiable, bool)` | добавить одну единицу |
| `AddOpenItem(Type|string, IIdentifiable, int, bool)` | добавить указанное количество |

## Текущие ограничения

- При добавлении существующий stack ищется только по `Id`, без проверки `Created`. Одинаковый `Id` у разных систем будет увеличивать первый найденный stack.
- `count` не валидируется: ноль и отрицательные значения передаются в `ItemStack.Add`.
- Методы не проверяют `selectableItem` на `null` и требуют готового `ProjectData`.
- `AddOpenItem` возвращает `true` после выполнения и не сообщает отдельно, был ли создан новый stack.
