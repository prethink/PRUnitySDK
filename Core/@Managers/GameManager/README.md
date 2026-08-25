# GameManager

`GameManager` координирует загрузку и сохранение `ProjectData` и `GameSettings`, публикует сигнал готовности данных и связывает жизненный цикл приложения с системой пауз SDK.

После инициализации контейнера менеджер доступен через:

```csharp
GameManager game = PRUnitySDK.Managers.Game;
```

## Инициализация

`InitializeGameManager()` можно вызывать повторно: после завершённой инициализации метод ничего не делает. Менеджер:

1. запоминает текущий `SynchronizationContext`;
2. получает `PRUnitySDK.GameDataStorage` и запускает `TryLoad()`;
3. после `gameDataStorage.ReadySignal` забирает `ProjectData` и `GameSettings`;
4. для первого запуска применяет значения из `PRUnitySDK.Settings.Default`;
5. запускает autosave, публикует `GameplayEvents.RaiseGameReady()` и переводит собственный `ReadySignal` в готовое состояние.

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
- сначала ожидает все `PRUnitySDK.Trackers.Saveables`;
- на главном потоке публикует `RaiseBeforeSaveEvent`, обновляет storage, вызывает `Save()` и затем `RaiseSaveEvent`.

Метод имеет сигнатуру `async void`, поэтому вызывающий код не может дождаться его завершения или получить исключение как `Task`. Исключения внутри сохранения логируются через `Debug.LogException`.

`SaveProjectData()` и `SaveGameSettingsData()` передают в storage только соответствующую модель с флагом немедленного обновления. Для согласованного сохранения обеих моделей и `ISaveable` используйте `StartSaveTask()`.

Все три пути обновляют диагностику менеджера. `SaveState` принимает значения `NotStarted`, `Saving`, `Succeeded` и `Failed`; `HasLoadedSave` сообщает, был ли при запуске успешно загружен существующий save. Стандартные storage сохраняют дату создания в `PRSaveData.SaveDate`, а дату записи — в `UpdateDate`, поэтому `SaveCreationTimeUtc` и `LastSaveTimeUtc` восстанавливаются после перезапуска. Для custom storage метаданные доступны через необязательный `IGameDataStorageSaveInfo`.

`CanStartSave()` проверяет параллельное сохранение и `SaveCooldownSeconds`, не изменяя состояние таймера. `SaveCooldownRemainingSeconds` отсчитывается от последней успешной save-операции и позволяет показать оставшееся время в UI. Обычный `StartSaveTask()` использует ту же проверку; overload с `isUserExecuter: true` по-прежнему явно обходит cooldown. Одновременные операции отображаются как `Saving`, пока не завершится последняя из них. Для платформенного storage `Succeeded` означает отсутствие синхронной ошибки при передаче данных, а не подтверждение удалённой cloud-записи: текущий `IGameDataStorage` не предоставляет такой callback.

Autosave включается настройкой `GameStorage.EnabledAutoSave` и ждёт `AutoSaveSeconds` через `WaitForSeconds`.

## Публичный API

| API | Назначение |
| --- | --- |
| `ReadySignal` | уведомляет, что storage загрузился и модели доступны |
| `GetProjectData()` | возвращает изменяемые данные проекта |
| `GetGameSettings()` | возвращает пользовательские настройки игры |
| `GetStorageSettings()` | возвращает `PRUnitySDK.Settings.GameStorage` |
| `StartSaveTask(bool)` | запускает полное асинхронное сохранение |
| `SaveProjectData()` | передаёт текущий `ProjectData` в storage |
| `SaveGameSettingsData()` | передаёт текущий `GameSettings` в storage |
| `SaveState` | состояние save-операций текущей сессии |
| `HasLoadedSave` | был ли существующий save успешно загружен в текущей сессии |
| `SaveCreationTimeUtc` | UTC-время создания текущего save или `null` |
| `LastSaveTimeUtc` | сохранённое UTC-время последней записи или `null` |
| `CanStartSave(bool)` | проверяет доступность полного сохранения без изменения cooldown |
| `SaveCooldownRemainingSeconds` | оставшееся время cooldown в целых секундах |
| `LoadDefaultControlSettings(...)` | применяет default control settings по текущей логике и при необходимости сохраняет |
| `LoadingUserCursorState()` / `ChangeCursorState()` | legacy-управление `Cursor.visible` через `GameSettings.IsShowCursor` |
| `OnPageVisibilityChange(int)` | WebGL/iOS-мост видимости страницы для системы пауз |

## Пауза и фокус

`OnApplicationPause` передаёт состояние в `PRUnitySDK.PauseManager.SetProjectPaused`, а `OnApplicationFocus` — в `SetFocusPaused`. `OnPageVisibilityChange` обрабатывается только для iOS и ожидает `0/1` от WebGL-моста.

Все три метода передают в `PauseManager` признак «нужна пауза»: скрытая страница (`isHidden = 1`) и потерянный фокус ставят паузу, видимая страница и полученный фокус — снимают.

## Текущие ограничения
- `GameSettingsSession` объявлен, но в этом partial-классе не создаётся. `OnStartScene()` вызывает у него `Reset()`, поэтому интеграция обязана инициализировать сессию до этого вызова.
- Методы управления курсором в `GameManager` являются legacy API; для конкурирующих UI-запросов используйте [CursorManager](../CursorManager/README.md).
