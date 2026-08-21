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
| `LoadDefaultControlSettings(...)` | применяет default control settings по текущей логике и при необходимости сохраняет |
| `LoadingUserCursorState()` / `ChangeCursorState()` | legacy-управление `Cursor.visible` через `GameSettings.IsShowCursor` |
| `OnPageVisibilityChange(int)` | WebGL/iOS-мост видимости страницы для системы пауз |

## Пауза и фокус

`OnApplicationPause` передаёт состояние в `PRUnitySDK.PauseManager.SetProjectPaused`, а `OnApplicationFocus` — в `SetFocusPaused`. `OnPageVisibilityChange` обрабатывается только для iOS и ожидает `0/1` от WebGL-моста.

## Текущие ограничения

- В `Start()` после `PreStart` повторно запускается `PostAwake`; стадия `PostStart` текущей реализацией не вызывается.
- `GameSettingsSession` объявлен, но в этом partial-классе не создаётся. `OnStartScene()` вызывает у него `Reset()`, поэтому интеграция обязана инициализировать сессию до этого вызова.
- Методы управления курсором в `GameManager` являются legacy API; для конкурирующих UI-запросов используйте [CursorManager](../CursorManager/README.md).
