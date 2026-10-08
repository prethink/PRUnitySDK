# GameManager

`GameManager` координирует загрузку и сохранение `ProjectData` и `GameSettings`, публикует сигнал готовности данных и связывает жизненный цикл приложения с системой пауз SDK.

После инициализации контейнера менеджер доступен через:

```csharp
GameManager game = PRUnitySDK.Managers.Game;
```

## Инициализация

`InitializeGameManager()` можно вызывать повторно: после завершённой инициализации метод ничего не делает. Менеджер получает `PRUnitySDK.GameDataStorage`, запускает `TryLoad()` и после `gameDataStorage.ReadySignal` забирает `ProjectData` и `GameSettings`. Для первого запуска он берёт значения из `PRUnitySDK.Settings.Default`. Затем запускает autosave, публикует `GameplayEvents.RaiseGameReady()` и переводит собственный `ReadySignal` в готовое состояние.

Наличие `PRUnitySDK.Managers.Game` ещё не означает, что сохранённые данные загружены. Код, читающий данные, должен дождаться `ReadySignal`:

```csharp
PRUnitySDK.Managers.Game.ReadySignal.SubscribeOnReady(() =>
{
    ProjectData data = PRUnitySDK.Managers.Game.GetProjectData();
});
```

`GetProjectData()` и `GetGameSettings()` выбрасывают `InvalidOperationException`, если вызваны до загрузки.

## Сохранение

Основной путь полного сохранения — `StartSaveTask()`:

- не запускает второе сохранение параллельно;
- для обычного вызова учитывает `PRUnitySDK.Settings.GameStorage.SaveCooldownSeconds`;
- `StartSaveTask(isUserExecuter: true)` обходит cooldown, но не защиту от параллельного сохранения;
- сначала синхронно собирает состояние всех `PRUnitySDK.Trackers.Saveables`: сломавшийся
  объект попадает в лог, но не отменяет сохранение остальных;
- на главном потоке публикует `RaiseBeforeSaveEvent`, обновляет storage, вызывает `Save()` и затем `RaiseSaveEvent`.

Метод имеет сигнатуру `async void`: дождаться его завершения нельзя. Исключения логируются через `Debug.LogException`.

`SaveProjectData(bool ignoreCooldown)` тоже идёт через `StartSaveTask()`. Часть состояния живёт в объектах сцены и попадает в сохранение только через `ISaveable.TrySaveData()`. Запись без сбора такого состояния теряет его и сдвигает cooldown.

`SaveFrequentProjectData()` — тот же полный путь с укороченным cooldown (`FrequentSaveCooldownSeconds`, 5 секунд от последней успешной записи). Нужен событиям, которые жалко терять, но которые идут пачками: новый уровень, награда платформы. Обычный вызов в cooldown отбрасывается, а `ignoreCooldown: true` на каждое такое событие упирается в предел площадки (у Яндекса `player.setData` — 100 запросов за 5 минут). Разовые действия игрока — покупка, подарок — по-прежнему пишутся через `SaveProjectData(true)`.

`SaveGameSettingsData()` передаёт в storage только настройки: от объектов сцены они не зависят.

Все три пути обновляют диагностику менеджера. `SaveState` принимает значения `NotStarted`, `Saving`, `Succeeded` и `Failed`; `HasLoadedSave` сообщает, был ли при запуске успешно загружен существующий save. Стандартные storage сохраняют дату создания в `PRSaveData.SaveDate`, а дату записи — в `UpdateDate`, поэтому `SaveCreationTimeUtc` и `LastSaveTimeUtc` восстанавливаются после перезапуска. Для custom storage метаданные доступны через необязательный `IGameDataStorageSaveInfo`.

`CanStartSave()` проверяет параллельное сохранение и `SaveCooldownSeconds`, не меняя таймер. `SaveCooldownRemainingSeconds` отсчитывается от последней успешной save-операции и подходит для UI. Обычный `StartSaveTask()` использует ту же проверку; overload с `isUserExecuter: true` обходит cooldown. Пока выполняется хотя бы одна операция, `SaveState` равен `Saving`. Для платформенного storage `Succeeded` означает отсутствие синхронной ошибки при передаче данных, а не подтверждение cloud-записи: `IGameDataStorage` такого callback не даёт.

Autosave включается настройкой `GameStorage.EnabledAutoSave` и ждёт `AutoSaveSeconds`.

## Публичный API

| API | Назначение |
| --- | --- |
| `ReadySignal` | уведомляет, что storage загрузился и модели доступны |
| `GetProjectData()` | возвращает изменяемые данные проекта |
| `GetGameSettings()` | возвращает пользовательские настройки игры |
| `GetStorageSettings()` | возвращает `PRUnitySDK.Settings.GameStorage` |
| `StartSaveTask(bool)` | запускает полное асинхронное сохранение |
| `SaveProjectData(bool)` | полное сохранение: сбор состояния сцены и запись, с возможностью обойти cooldown |
| `SaveGameSettingsData()` | передаёт текущий `GameSettings` в storage |
| `SaveState` | состояние save-операций текущей сессии |
| `HasLoadedSave` | был ли существующий save успешно загружен в текущей сессии |
| `SaveCreationTimeUtc` | UTC-время создания текущего save или `null` |
| `LastSaveTimeUtc` | сохранённое UTC-время последней записи или `null` |
| `CanStartSave(bool)` | проверяет доступность полного сохранения без изменения cooldown |
| `SaveCooldownRemainingSeconds` | оставшееся время cooldown в целых секундах |
| `LoadDefaultControlSettings(...)` | применяет default control settings по текущей логике и при необходимости сохраняет |
| `OnPageVisibilityChange(int)` | WebGL/iOS-мост видимости страницы для системы пауз |

## Пауза и фокус

`OnApplicationPause` передаёт состояние в `PRUnitySDK.PauseManager.SetProjectPaused`, а `OnApplicationFocus` — в `SetFocusPaused`. `OnPageVisibilityChange` обрабатывается только для iOS и ожидает `0/1` от WebGL-моста.

Все три метода передают в `PauseManager` признак «нужна пауза»: скрытая страница (`isHidden = 1`) и потерянный фокус ставят паузу, видимая страница и полученный фокус — снимают.

## Текущие ограничения
- `GameSettingsSession` объявлен, но в этом partial-классе не создаётся. `OnStartScene()` вызывает у него `Reset()`, поэтому интеграция обязана инициализировать сессию до этого вызова.
- Методы управления курсором в `GameManager` являются legacy API; для конкурирующих UI-запросов используйте [CursorManager](../CursorManager/README.md).
