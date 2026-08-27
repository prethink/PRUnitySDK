# Трекеры PRUnitySDK

Трекеры хранят runtime-объекты, которым нужен общий реестр: игроков, сущности,
камеры, окна, уведомители и watcher-свойства. Основная точка доступа:

```csharp
PRUnitySDK.Trackers
```

## Доступные трекеры

| Свойство | Тип | Назначение |
| --- | --- | --- |
| `Players` | `PlayerTracker` | Игроки, локальные слоты и Player ID |
| `Entities` | `EntityTracker` | Сущности **кроме игроков** и статистика по их типам |
| `CameraTracker` | `CameraTracker` | Стек контроллеров и игровые камеры |
| `MonoWindows` | `MonoWindowsTracker` | UI-окна с уникальными ключами |
| `Notifiers` | `NotifierTracker` | UI-уведомители с уникальными ключами |
| `BackgroundTasks` | `BackgroundTaskTracker` | Фоновые задачи по расписанию |

## Общий контракт

`TrackerBase<T>` определяет три основные операции:

```csharp
bool registered = tracker.Register(element);
bool removed = tracker.Unregister(element);
bool contains = tracker.Contains(element);
```

`Register()` возвращает `false`, если значение недопустимо, уже зарегистрировано
или конфликтует с правилами конкретного трекера. `Unregister()` возвращает `false`,
если элемент не был найден. Это позволяет обрабатывать ожидаемый отказ без исключения.

`Elements` возвращает снимок списка. Изменение полученной коллекции не изменяет
содержимое трекера и не заменяет вызов `Register()` или `Unregister()`.

## PlayerTracker

`PlayerTracker` назначает каждому игроку глобальный Entity ID и отдельный Player ID.
Освобождённые Player ID переиспользуются, что соответствует компактной нумерации
игроков в духе классических серверных систем.

```csharp
PlayerTracker players = PRUnitySDK.Trackers.Players;

int activePlayers = players.PlayersCount;
PlayerLocal firstLocal = players.GetLocalPlayer(0);
```

Для локальных игроков доступно не более `MaxLocalPlayer` слотов. Регистрация нового
локального игрока вернёт `false`, если все слоты заняты. При удалении игрока его
Player ID и локальный слот освобождаются.

`Clear()` полностью уничтожает зарегистрированных игроков и сбрасывает идентификаторы.

## EntityTracker

`EntityTracker` назначает Entity ID, хранит сущности и считает регистрации по
`EntityType`. Методы `GetExact...` фильтруют по точному типу, а `GetInherited...` —
по совместимости CLR-типа.

> **Игроков здесь нет.** `PlayerBase` переопределяет `RegisterEntity()` и регистрирует себя
> только в `PlayerTracker`, не вызывая базовую реализацию. Поэтому обход `Entities` игроков
> не вернёт, счётчик по типу `Player` будет нулевым, а `Clear()`, `ClearRound()` и
> `ClearSession()` их не затронут — те же методы нужно вызвать и у `PlayerTracker`.
> Entity ID при этом сквозной: оба трекера берут его из общего `EntityIdGenerator`,
> так что идентификаторы не пересекаются.

Регистрация не снимается, когда сущность уходит в пул: объект остаётся в реестре, меняется
только флаг `InPool`. Именно поэтому есть отдельные счётчики «на сцене» и «в пуле».

```csharp
long enemies = PRUnitySDK.Trackers.Entities.GetExactExistsEntityCount(enemyType);
```

`ClearRound()` удаляет сущности с временем жизни `Round`, `ClearSession()` — сначала
сущности сессии, затем раунда, а `Clear()` уничтожает все сущности.

## CameraTracker

`CameraTracker` использует LIFO-стек контроллеров камер. `Push()` помещает контроллер
на вершину, `Peek()` возвращает текущий живой контроллер, `Pop()` извлекает его, а
`RestorePreviousCamera()` восстанавливает предыдущую камеру.

Уничтоженные Unity-объекты пропускаются и удаляются при обходе. `PlayerCameras`
возвращает снимок набора, поэтому его можно безопасно перечислять вне трекера.

## MonoWindowsTracker и NotifierTracker

Окна и уведомители регистрируются с уникальным `Key`. Повторный объект или второй
объект с тем же ключом не регистрируется.

Подробное создание, открытие и закрытие окон описано в [MonoWindow](../%23UI/MonoWindow/README.md).

```csharp
PRUnitySDK.Trackers.MonoWindows.TryShowWindow(windowKey);

if (PRUnitySDK.Trackers.Notifiers.TryGetNotifier(notifierKey, out DamageNotifier notifier))
    notifier.Show();
```

Методы поиска безопасно возвращают `false` или `null`, если объект не найден либо был
уничтожен Unity.

## BackgroundTaskTracker

Задача должна иметь непустой уникальный строковый ключ. Трекер не заводит на неё
отдельную корутину: он сам встаёт в тиковый цикл `PRMonoBehaviourHost` и на каждом
проходе выполняет только те задачи, у которых истёк интервал. До готовности SDK
(`PRUnitySDK.IsInitialized`) запуски не происходят.

Подробности расписания, лимитов и диагностики — в
[BackgroundTasks](../BackgroundTasks/README.md).

## Рекомендации владельцу объекта

- Регистрируйте объект только после заполнения его ключевых данных.
- Симметрично вызывайте `Unregister()` при уничтожении объекта.
- Решите заранее, что делать при возврате в пул: `EntityBase` регистрацию **сохраняет**
  и помечает объект флагом `InPool`, а окна и уведомители снимаются полностью. Оба варианта
  допустимы, но смешивать их в одном трекере нельзя — иначе счётчики перестанут сходиться.
- Не изменяйте внутренние коллекции в обход публичного API.
- Проверяйте результат `Register()`, если возможны дубликаты или занятые слоты.
- Помните, что `Elements` и снимки вроде `Entities` / `Players` создают новый список
  на каждом обращении — не вызывайте их в `Update`.

## Смотрите также

- [Entity](../@Entity/README.md) — сущности, их регистрация, время жизни и пул
- [MonoWindow](../%23UI/MonoWindow/README.md) — окна и их ключи
