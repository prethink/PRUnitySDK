# Reward и выдача наград

Reward-модель отделяет описание награды от способа её фактической выдачи:

- `RewardResource` добавляет ресурс в общий Wallet;
- `RewardAction` выполняет настроенный `ActionBase`;
- `RewardItem` оборачивает произвольный `ItemDefinitionBase`;
- `RewardContainerBase` позволяет контейнеру наград самому быть наградой.

При программном или Editor-создании используйте `RewardItem.Initialize`, `RewardResource.Initialize` и `RewardAction.Initialize`. Генератору не нужно обращаться к внутренним именам сериализованных полей.

## Что лежит внутри награды

`RewardItemCollector` разбирает награду на предметы, спускаясь по вложенным контейнерам:

```csharp
IEnumerable<string> ids = RewardItemCollector.GetItemIds(reward);
```

Нужен тем, кто хочет знать состав, не выдавая награду: так системы сообщают
`ReservedItemsManager`, что они раздают. Повторно встреченные награды пропускаются,
поэтому кольцо ссылок между контейнерами разбор не зациклит.

## Получение сервиса

`RewardGrantService` создаётся отдельным SDK-модулем на стадии `MethodHookStage.SDK`, регистрируется как `IRewardGrantService` и доступен через:

```csharp
IRewardGrantService rewards = PRUnitySDK.RewardGrantService;
```

Обычная выдача:

```csharp
bool granted = PRUnitySDK.RewardGrantService.TryGrant(
    reward,
    executor: player.PlayerId);
```

Для умноженной награды можно передать множитель. Сам сервис не запускает рекламу и не показывает UI:

```csharp
PRUnitySDK.RewardGrantService.TryGrant(
    reward,
    executor: player.PlayerId,
    multiplier: 3);
```

Решение о просмотре рекламы остаётся в Advertising или окне награды. Множитель применяется обработчиком ресурса и может игнорироваться обработчиком уникального предмета.

## Контекст игрока

`RewardGrantContext` содержит награду, `Executor`, необязательную прямую ссылку на `IPlayer`, множитель и флаг сохранения. Обработчик персональной награды может вызвать `TryGetPlayer`, чтобы получить правильного игрока в split-screen:

```csharp
var context = new RewardGrantContext(
    reward,
    player.PlayerId,
    multiplier: 1,
    save: true,
    player: player);

PRUnitySDK.RewardGrantService.TryGrant(context);
```

## Обработчики

Стандартный сервис регистрирует:

- `RewardResourceGrantHandler`;
- `RewardActionGrantHandler`;
- fallback `RewardItemGrantHandler`, добавляющий предмет в `OpenedItemsManager`.

Обработчики проверяются по убыванию `Priority`. Первый подходящий обработчик полностью отвечает за выдачу. Благодаря этому private-модуль может заменить fallback-поведение для своего типа definition.

```csharp
public sealed class PetRewardGrantHandler : IRewardGrantHandler
{
    public int Priority => 1000;

    public bool CanHandle(RewardGrantContext context)
    {
        return context?.Reward is RewardItemBase itemReward &&
               itemReward.Item is SomeItemDefinition;
    }

    public bool TryGrant(RewardGrantContext context)
    {
        var reward = (RewardItemBase)context.Reward;
        return SomeUnlockService.TryUnlock((SomeItemDefinition)reward.Item);
    }
}
```

Регистрация выполняется после создания общего сервиса, обычно private partial-hook с priority больше `60`:

```csharp
[MethodHook(MethodHookStage.SDK, 65)]
private static void InitializePetRewardHandler()
{
    PRUnitySDK.RewardGrantService.RegisterHandler(new PetRewardGrantHandler());
}
```

Один конкретный тип обработчика повторно не регистрируется.

## Событие успешной выдачи

`IRewardGrantedEvent.OnRewardGranted(RewardGrantContext)` вызывается только после успешной выдачи. Это уведомление для UI, аналитики и дополнительных реакций; оно не используется вместо обработчика.

```csharp
public void OnRewardGranted(RewardGrantContext context)
{
    Debug.Log($"Granted: {context.Reward.name}");
}
```

Если обработчик отсутствует, награда не считается выданной, событие не отправляется, а `TryGrant` возвращает `false`.

## Фильтрация коллекций

`RewardCollectionExtensions` заменяет старый Zenject-era `RewardUtils`:

```csharp
IEnumerable<RewardResource> resources = rewards.GetOnlyResources();
IEnumerable<RewardItemBase> items = rewards.GetOnlyItems();
IEnumerable<RewardDataBase> configured = rewards.GetConfiguredRewards();

IEnumerable<RewardDataBase> available = rewards.GetAvailableRewards(
    itemReward => ownership.IsOpened(itemReward.Item));
```

Правило владения передаётся снаружи, потому что разные проекты могут хранить открытые brainrots, pets и предметы кастомизации в разных разделах сохранения.

## Зависимости

| От чего зависит | Зачем |
| --- | --- |
| [Wallet](../Wallet/README.md) и `ResourceManager` | выдача ресурсов |
| [OpenedItemsManager](../@Managers/OpenedItemsManager/README.md) | отметка о выданных предметах |
| `@Actions` | награда-действие выполняет настроенный `ActionBase` |
| [ProjectPropertiesManager](../@Managers/ProjectPropertiesManager/README.md) | сроки у наград с ограничением по времени |

Кто зависит от него: достижения, подарки, кейсы и всё, что что-то выдаёт. Обработчик
выдачи подключается со стороны — сама модель наград о них не знает.


## Ограниченные по времени награды

`TimeLimitedRewardBase` — база для наград, действующих до определённого момента: VIP, бустеры ресурсов. Состояние хранит `TimeLimitedRewardService` в отдельном наборе данных `ProjectData.TimeLimitedRewards`, источник времени — `PRUnitySDK.ServerTime`.

```csharp
if (vipManager.IsActive(out DateTime endTime))
    ShowVipBadge(endTime);

vipManager.AddTime(TimeSpan.FromDays(7));   // продлит активный VIP или выдаст заново
```

| Метод базы | Смысл |
| --- | --- |
| `IsActive(out endTime)` | действует ли награда с ключом `Name` |
| `GetRemaining()` | сколько осталось, ноль у истёкшей |
| `AddTime(duration)` | продлить активную либо начать новый период от текущего времени |
| `Remove()` | снять досрочно |
| `GetName(name)` | преобразование логического имени в ключ хранилища |

### Сервис

`TimeLimitedRewardService` работает с ключами напрямую и умеет то, чего не было раньше:

| Метод | Назначение |
| --- | --- |
| `GetActive()` | все действующие награды списком |
| `TryGetState(key, out state)` | состояние награды, включая истёкшую |
| `RemoveExpired()` | снять истёкшие и опубликовать событие окончания |
| `SetEndTime(key, endTime)` | задать момент окончания напрямую |
| `Clear()` | снять все награды |

### События

| Интерфейс | Когда |
| --- | --- |
| `ITimeLimitedRewardChangedEvent` | награда выдана или продлена; `wasActive` отличает продление от новой выдачи |
| `ITimeLimitedRewardExpiredEvent` | награда снята или истекла при `RemoveExpired()` |

Об окончании узнать можно только через `RemoveExpired()` — вызывайте его периодически из игрового цикла, иначе UI не погасит иконку до следующей проверки `IsActive`.

### Почему отдельный набор данных

Раньше момент окончания лежал в `ProjectProperties.DateTimeProperties` вместе с произвольными датами. Из этого следовало три ограничения: награды нельзя было перечислить, об истечении никто не узнавал, а ключ мог совпасть с чужим свойством: тот, кто строит ключ конкатенацией (`"Coins" + "_booster"`), рискует получить чужое одноимённое DateTime-свойство поверх своего.

Формат сохранения при переходе изменился: награды, записанные старой версией, не читаются.

Для наград с несколькими логическими ключами наследник может использовать защищённые перегрузки `IsActive(name, ...)` и `AddTime(name, ...)`, а `GetName(name)` — добавить стабильный prefix/postfix к ключу сохранения. Так награда хранит отдельный срок для каждого своего ключа — например, по типу ресурса, — не дублируя алгоритм работы со временем.
