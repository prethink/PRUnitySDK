# MonoWindow

`MonoWindow` — система модальных runtime-окон PRUnitySDK. Она состоит из:

- `MonoWindowBase` — базового компонента окна;
- `MonoWindowsTracker` — реестра окон и единой точки переключения;
- `MonoWindowFactoryBase<T>` — фабрики prefab из `Resources`;
- `MonoWindowArgs` — параметров, передаваемых при открытии;
- `MonoWindowKeyEnumerationProvider` — набора уникальных ключей.

Одновременно отображается только одно окно. Открывать окна рекомендуется через
`PRUnitySDK.Trackers.MonoWindows`, хотя прямой вызов `Show()` также закроет остальные зарегистрированные окна.

## Создание ключа

Ключи объявляются в partial-классе провайдера:

```csharp
public partial class MonoWindowKeyEnumerationProvider
{
    public static readonly Enumeration Inventory = new(nameof(Inventory));
}
```

Окна с повторяющимися или пустыми ключами не регистрируются. При повторяющемся ключе в консоль выводится
предупреждение с именем уже зарегистрированного объекта.

## Создание окна

```csharp
public sealed class InventoryWindow : MonoWindowBase
{
    public override Enumeration Key => MonoWindowKeyEnumerationProvider.Inventory;

    public override void InitTranslate()
    {
        // Обновление локализованных элементов конкретного окна.
    }

    public override void Show(MonoWindowArgs args)
    {
        base.Show(args);

        if (args.TryGetData<InventoryWindowData>(out var data))
            DrawInventory(data);
    }

    private void DrawInventory(InventoryWindowData data)
    {
        // Заполнение UI.
    }
}
```

В Inspector у окна можно назначить:

- `Container` — объект, который включается и выключается; если ссылка отсутствует, используется `gameObject`;
- `Exit Button` — кнопку обычного закрытия;
- `Set Pause When Open` — необходимость поставить игровую логику на паузу.

Обработчик кнопки добавляется системой без удаления обработчиков, назначенных другими компонентами или prefab.
`InitTranslate()` вызывается при каждом открытии окна перед обновлением layout.

## Фабрика

Prefab окна должен находиться внутри папки `Resources`.

```csharp
public sealed class InventoryWindowFactory : MonoWindowFactoryBase<InventoryWindow>
{
    public override bool UseSharedCanvas => true;
    public override bool WorldPositionStays => false;
    public override bool IsSingleton => true;
    public override string ResourcePath => "PRUnitySDK/Prefabs/Windows/MonoWindows/InventoryWindow";
}
```

Фабрика возвращает `null` и пишет понятную ошибку в консоль, если путь пуст, prefab отсутствует или контейнер окон
ещё не создан. Родитель назначается непосредственно при создании экземпляра.

## Открытие и закрытие

Окно без дополнительных данных:

```csharp
bool shown = PRUnitySDK.Trackers.MonoWindows.TryShowWindow(
    MonoWindowKeyEnumerationProvider.Inventory);
```

Окно с типизированными данными и идентификатором исполнителя:

```csharp
var args = new MonoWindowArgs<InventoryWindowData>(inventoryData)
{
    Executor = localPlayerId
};

bool shown = PRUnitySDK.Trackers.MonoWindows.TryShowWindow(
    MonoWindowKeyEnumerationProvider.Inventory,
    args);
```

`TryShowWindow()` возвращает `false`, если ключ пуст или окно не зарегистрировано. В этом случае уже открытое окно
не закрывается. При успешном поиске остальные видимые окна закрываются, после чего отображается требуемое.

```csharp
PRUnitySDK.Trackers.MonoWindows.HideAllWindows();
PRUnitySDK.Trackers.MonoWindows.HideForceAllWindows();
```

Обычное закрытие запускает сохранение данных. Принудительное закрытие предназначено для смены сцены, сброса
сессии и аварийного завершения UI, поэтому сохранение не запускает.

Текущее состояние доступно через:

```csharp
MonoWindowBase current = PRUnitySDK.Trackers.MonoWindows.CurrentWindow;
bool hasOpenWindows = PRUnitySDK.Trackers.MonoWindows.HasOpenWindows;
bool isWindowOpen = PRUnitySDK.IsWindowOpen;
```

## Диагностика в PRUnitySDK Debug

В Play Mode вкладка `Windows` показывает все окна, зарегистрированные в
`PRUnitySDK.Trackers.MonoWindows`: фактический тип, key, `IsVisible`, активность GameObject и
соответствие `CurrentWindow`. `Object` выбирает экземпляр в Hierarchy/Inspector, а `Source` —
его MonoScript.

Кнопка `Close` вызывает принудительное закрытие без запуска сохранения. `Open` использует
`MonoWindowArgsEmpty`; для окна, требующего обязательные типизированные данные, используйте
обычный игровой сценарий открытия. Вкладка `Problems` дополнительно сообщает о повторяющихся
ключах, нескольких одновременно видимых окнах и невидимом `CurrentWindow`.

## EventBus

Трекер реализует `IMonoWindowEvents` и автоматически подписывается на `EventBus`. Поэтому окно можно открыть
без прямой зависимости от трекера:

```csharp
EventBus.RaiseEvent<IMonoWindowEvents>(events =>
    events.TryShowWindow(MonoWindowKeyEnumerationProvider.Inventory.Value));
```

Для обычного игрового кода предпочтителен прямой вызов трекера, поскольку он позволяет проверить результат
`TryShowWindow()`. EventBus удобен для слабосвязанных систем, которым результат открытия не нужен.

`TriggerMonoWindow` автоматически записывает `PlayerId` вошедшего локального игрока в `Executor`. Поиск игрока
выполняется через `Collider.TryGetLocalPlayer()` и `PlayerLink`, поэтому используется общий для проекта способ
разрешения обычных, дочерних hitbox и ragdoll-collider.

## Пауза и курсор

Окно снимает логическую паузу только в том случае, если оно само установило её при открытии. Если игра уже была
на паузе, окно не присваивает эту паузу себе и не снимает её при закрытии. Если другая система изменяет
логическую паузу во время открытого окна, владение передаётся ей; после снятия внешней паузы открытое окно снова
устанавливает свою паузу.

При открытии курсор становится видимым. Пользовательское состояние курсора восстанавливается после закрытия
последнего видимого окна.

## Рекомендации

- всегда вызывайте `base.Show(args)` и `base.Hide(isForceClose)` в переопределениях;
- используйте уникальный стабильный `Enumeration`-ключ;
- используйте `TryGetData<T>()`, если отсутствие или другой тип данных допустимы;
- используйте `GetData<T>()`, если неверный тип является ошибкой контракта;
- не сохраняйте ссылку на `MonoWindowArgsEmpty`: создавайте пустые параметры через трекер;
- для закрытия при смене сцены используйте `HideForceAllWindows()`.

## Готовые окна

Ядро окон не содержит: каждое окно живёт в своём модуле проектного слоя и подключается
partial-файлом `PRWindowsContainer`. Здесь описан только контракт `MonoWindowBase`,
трекер окон и параметры показа.
