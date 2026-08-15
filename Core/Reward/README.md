# Reward и выдача наград

Reward-модель отделяет описание награды от способа её фактической выдачи:

- `RewardResource` добавляет ресурс в общий Wallet;
- `RewardAction` выполняет настроенный `ActionBase`;
- `RewardItem` оборачивает произвольный `ItemDefinitionBase`;
- `RewardContainerBase` позволяет контейнеру наград самому быть наградой.

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
               itemReward.Item is PetDefinition;
    }

    public bool TryGrant(RewardGrantContext context)
    {
        var reward = (RewardItemBase)context.Reward;
        return PetUnlockService.TryUnlock((PetDefinition)reward.Item);
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
