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
Текст привязывается через `SetLocalization`; `LocalizationObserver` обновляет его при смене языка.

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

### Окна и постоянный интерфейс — разные canvas

`PRWindowsContainer` поднимает два экранных canvas с явным порядком отрисовки:

| Контейнер | `sortingOrder` | Что на нём |
| --- | --- | --- |
| `HudCanvas` | 0 | Постоянный интерфейс: полосы, панель быстрого доступа |
| `SharedCanvas` | 100 | Окна с `UseSharedCanvas` |

Порядок задан явно, потому что внутри одного canvas его решает иерархия: постоянный
интерфейс включается со сцены, то есть **позже** окон, и на общем canvas он накрывал бы
открытое окно собой. Окна теперь выше при любом порядке создания.

Постоянный интерфейс прячется целиком:

```csharp
PRUnitySDK.Windows.HideHud();
PRUnitySDK.Windows.ShowHud();
```

Состояние и список элементов держит `HudTracker` (`PRUnitySDK.Trackers.Hud`), а `HideHud` —
обёртка над ним. Через трекер интерфейс проходит потому, что
полосы над сущностями висят в мире, каждая на своём canvas, и общим выключателем их
не достать: каждый элемент реализует `IHudElement` и получает состояние по регистрации.
Текущее состояние лежит в `IsHudVisible`: интерфейс, созданный уже после
скрытия, спрашивает его сам и не выходит на экран.

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

Трекер сообщает о показе и скрытии окон через `IMonoWindowVisibilityEvent`
(`OnWindowShown`, `OnWindowHidden`). «Скрыто» приходит только для окна, о показе которого
уже сообщалось: выключение никогда не открытого окна событием не считается. Повторное
включение показанного окна второго «показано» не даёт.

Трекер реализует `IMonoWindowEvents` и автоматически подписывается на `EventBus`. Поэтому окно можно открыть
без прямой зависимости от трекера:

```csharp
EventBus.RaiseEvent<IMonoWindowEvents>(events =>
    events.TryShowWindow(MonoWindowKeyEnumerationProvider.Inventory.Value));
```

Для обычного игрового кода предпочтителен прямой вызов трекера, поскольку он позволяет проверить результат
`TryShowWindow()`. EventBus удобен для слабосвязанных систем, которым результат открытия не нужен.

`TriggerMonoWindow` автоматически записывает `PlayerId` вошедшего игрока в `Executor`. По умолчанию игрок ищется
через `GetComponentInParent<PlayerBase>()`. Кто именно имеет право открыть окно — правило проектное: hook'и стадии
`TriggerMonoWindow.ExecutorStage` с сигнатурой `(Collider other, ref long executor, ref bool hasExecutor)` могут
назначить своего исполнителя или запретить открытие. В этом проекте такой hook оставляет только локального игрока
и ищет его общим для проекта способом — по обычным, дочерним hitbox и ragdoll-коллайдерам.

## Переход: появление и закрытие

Окно появляется и закрывается переходом: по умолчанию выпрыгивает из центра и сжимается
обратно, проявляясь вместе с затемнением. Анимация на DOTween и идёт на unscaled time —
окна открываются как раз на паузе.

Общая настройка — `PRSDKSettings → Window Transition` (`MonoWindowTransitionSettings`):

| Поле | Что задаёт |
| --- | --- |
| `Enabled` | переход вообще; выключено — окна «по умолчанию» открываются сразу |
| `Preset` | готовый переход (таблица ниже) или `Custom` |
| `Custom` | свои значения; видны и работают только при `Preset = Custom` |

| Пресет | Как выглядит |
| --- | --- |
| `Pop` | по умолчанию: вырастает из нуля с лёгким перелётом (`OutBack`, 0.3 с), сжимается (`InBack`, 0.15 с) |
| `Soft` | мягко проявляется, подрастая с 90 % |
| `Fade` | только проявляется и гаснет |
| `SlideUp` | въезжает снизу и уезжает вниз |
| `SlideDown` | въезжает сверху и уезжает вверх |
| `Elastic` | пружинит, выскакивая из центра |

Свои значения (`MonoWindowTransition`): длительность и кривая появления и закрытия (с графиком
кривой), `Hidden Scale` — из какого масштаба растёт (1 — размер не меняется), `Fade` —
проявлять контейнер целиком, `Slide Offset` — откуда въезжает, в долях размера цели:
`(0, -0.35)` — снизу на треть высоты. Новое значение `Custom` начинается с `Pop`.

У окна в Inspector, раздел «Переход»:

- `Transition Mode` — `Default` (из настроек проекта), `Override` (свой `Transition Override`:
  другой пресет или свои значения) или `None` (без перехода);
- `Transition Target` — что масштабировать и двигать, обычно панель окна. Пусто — контейнер
  целиком, и тогда растёт и затемнение. Пивот цели — в центре, иначе окно вырастет из угла.

Решить в коде — переопределить `GetTransition()`. Он зовётся на каждое открытие и закрытие,
`null` — без перехода:

```csharp
protected override MonoWindowTransition GetTransition() => null;

protected override MonoWindowTransition GetTransition() =>
    MonoWindowTransition.FromPreset(MonoWindowTransitionPreset.SlideUp);
```

Что важно знать:

- состояние меняется сразу, не дожидаясь анимации: закрываемое окно тут же отпускает паузу
  и курсор, а `IsVisible` у него `false`, хотя оно ещё сжимается. Поэтому действие после
  `Hide()` (например, реклама) не ждёт перехода, а открытое следом окно не закрывает уходящее;
- контейнер выключается в конце перехода. Код, которому нужен выключенный контейнер сразу
  после `base.Hide()`, должен закрывать окно принудительно или отключать ему переход;
- принудительное закрытие (`Hide(true)`, `HideForceAllWindows`) перехода не играет;
- повторный показ уже открытого окна перехода не играет, а показ во время закрытия
  разворачивает окно оттуда, куда оно успело уйти;
- место цели переход трогает, только если у него есть `Slide Offset`. Цель, которую ставит
  `LayoutGroup`, въезжать не сможет — разметка вернёт её на место;
- для `Fade` окно берёт `CanvasGroup` контейнера или добавляет его. Во время закрытия
  `blocksRaycasts` выключен: нажатие по уходящему окну не проходит.

Окна, построенные кодом (`IndexWindowBase`, `RewardContainerWindowBase`), сами задают
цель перехода — свою панель.

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

Отключение компонента окна, его GameObject или родителя освобождает принадлежащие
окну паузу и запрос курсора без сохранения. При повторном включении ранее показанное
окно восстанавливает эти запросы, если его контейнер виден. Уничтожение также освобождает
состояние. `IsVisible` учитывает активность всей иерархии и самого компонента.
