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

## Гарант редкой награды и показ шансов

У контейнера (`RewardContainerBase`) две настройки розыгрыша, обе видны в его инспекторе:

| Поле | Что делает |
| --- | --- |
| `Pity` | Гарант (`RewardPitySettings`): шанс редких наград растёт с каждым открытием без них, а на заданном по счёту открытии редкая гарантирована. Выключен — розыгрыш только по весам |
| `Show Chances` | Окно, которое разыгрывает контейнер, подписывает каждой награде её шанс |

Настройки гаранта: `Enabled`, `Rare Quality` (награда этого качества и выше считается редкой),
`Guarantee After` (10 — девять неудач подряд, десятая точно редкая), `Chance Growth` (прибавка
к весу редких за каждую неудачу; ноль — растёт только счётчик до гарантии).

```csharp
// итог, который достанется игроку: по весам с учётом гаранта
if (container.TryRollWithPity(out RewardBase reward))
{
    PRUnitySDK.RewardGrantService.TryGrant(reward, executor);
    container.RegisterRoll(reward);   // редкая обнуляет счётчик, обычная прибавляет
}

string label = RewardPity.FormatChance(container.GetChance(reward));   // «12%», «0.5%»
```

Выбрать и выдать — разные моменты, поэтому `TryRollWithPity` счётчик только читает, а двигает его
`RegisterRoll`. Обычный `TryRoll` гаранта не знает: им разыгрывают карточки-попутчики и выигрыши ботов.
Счётчик у каждого розыгрыша свой и лежит в свойствах проекта под ключом `RewardPity.<id>`.
Тот, у кого наград нет в контейнере (колесо удачи), зовёт `RewardPity` напрямую со своим списком,
фильтром и идентификатором.

Гарант действует, только пока среди доступных наград есть и редкие, и обычные: собрал игрок все
редкие предметы — обещать нечего, и шанс считается по обычным весам. `GetChance` считает тем же
расчётом, что и розыгрыш, поэтому подпись обещает ровно то, что разыграет следующая попытка.

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

`RewardCollectionExtensions` заменяет прежний `RewardUtils`:

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

Награду можно выдать навсегда: `TimeLimitedRewardService.SetPermanent(key)` (у наследника базы —
`SetPermanent(name)`). Бессрочная — та же временная с предельной датой окончания
(`PermanentEndTime`); узнать её можно через `TimeLimitedRewardService.IsPermanent(endTime)`.
`AddTime` бессрочную не меняет. Тот, кто показывает срок игроку, должен проверять её сам:
пересчёт предельной даты в местное время бросает исключение.

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
| `ITimeLimitedRewardExpiredEvent` | награда снята досрочно или её срок вышел |

Событие окончания приходит само. Фоновая задача `TimeLimitedRewardExpiryTask` раз в секунду вызывает `RemoveExpired()`, так что UI достаточно подписаться на `ITimeLimitedRewardExpiredEvent` и следить за сроками самому не нужно.

| Что | Как |
| --- | --- |
| Начало | после `GameManager.ReadySignal`; первый запуск снимает и то, что истекло, пока игра была закрыта |
| Пауза | задача идёт по реальному времени и на логической паузе продолжает работать |
| Задержка | не больше `TimeLimitedRewardExpiryTask.CheckIntervalSeconds` (1 с) после срока |
| Сохранение | только если что-то снято |
| Отладка | окно `PRUnitySDK Debug`, вкладка Tasks: там задачу можно запустить сразу |

В промежутке между сроком и тиком задачи `IsActive` уже возвращает `false`, а события ещё не было. Если нужна точность до кадра, проверяйте `IsActive` или вызовите `RemoveExpired()` сами.

### Почему отдельный набор данных

Раньше момент окончания лежал в `ProjectProperties.DateTimeProperties` вместе с произвольными датами. Из этого следовало три ограничения: награды нельзя было перечислить, об истечении никто не узнавал, а ключ мог совпасть с чужим свойством: тот, кто строит ключ конкатенацией (`"Coins" + "_booster"`), рискует получить чужое одноимённое DateTime-свойство поверх своего.

Формат сохранения при переходе изменился: награды, записанные старой версией, не читаются.

Для наград с несколькими логическими ключами наследник может использовать защищённые перегрузки `IsActive(name, ...)` и `AddTime(name, ...)`, а `GetName(name)` — добавить стабильный prefix/postfix к ключу сохранения. Так награда хранит отдельный срок для каждого своего ключа — например, по типу ресурса, — не дублируя алгоритм работы со временем.
