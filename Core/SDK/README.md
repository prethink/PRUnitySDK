# SDK

Папка содержит центральный facade `PRUnitySDK`, процесс инициализации, service resolver,
настройки, базу данных и ScriptableObject-singleton инфраструктуру.

## PRUnitySDK facade

Основные точки доступа:

| Свойство | Назначение |
| --- | --- |
| `PRUnitySDK.Settings` | Проектные настройки `PRSDKSettings` |
| `PRUnitySDK.Database` | Общая база `PRSDKDatabase` |
| `PRUnitySDK.Utils` | Набор SDK-утилит |
| `PRUnitySDK.Trackers` | Runtime-трекеры объектов |
| `PRUnitySDK.Managers` | Контейнер менеджеров |
| `PRUnitySDK.Windows` | Контейнер окон |
| `PRUnitySDK.ResourcePaths` | Централизованные Resources-пути |
| `PRUnitySDK.PauseManager` | Сервис паузы |
| `PRUnitySDK.ReadySignal` | Сигнал завершения инициализации |

## Инициализация

Обычная точка входа — компонент `Bootstrap`, вызывающий:

```csharp
PRUnitySDK.InitializeSDK();
```

Последовательность:

1. Защита от повторного запуска через `IsStartInitialize`.
2. Инициализация `GameRules`.
3. Регистрация JSON-конвертеров.
4. Инициализация `PRMonoBehaviourHost` и `PRTimeScale`.
5. Регистрация фабрик.
6. Выполнение static method hooks этапа `SDK`.
7. Инициализация контейнеров менеджеров и окон.
8. Установка `IsInitialized`.
9. Публикация `ISDKEvents.OnInitialized()`.
10. Перевод `ReadySignal` в готовое состояние.

```csharp
if (PRUnitySDK.IsInitialized)
{
    StartGame();
}
```

Не вызывайте `InitializeSDK()` параллельно. После неудачи `IsStartInitialize` остаётся
установленным, поэтому автоматической повторной попытки текущая реализация не выполняет.

## Service resolver

```csharp
PRUnitySDK.RegisterService<IMyService>(new MyService());

IMyService service = PRUnitySDK.ResolveService<IMyService>();

if (PRUnitySDK.TryResolve<IMyService>(out var optionalService))
{
    optionalService.Execute();
}
```

`RegisterService` поддерживается только стандартным `ServiceResolver`. Если resolver
переопределён интеграцией, регистрация этим методом выбрасывает исключение.

Модули SDK регистрируются через method hooks и `InitializeModuleSDK`. Повторная
инициализация типа отслеживается в `InitializedTypes`.

## Настройки и база данных

`PRSDKSettings` и `PRSDKDatabase` наследуются от `ScriptableObjectSingleton<T>`. Поиск
выполняется через `Resources` и `PRUnitySDK.ResourcePaths.CorePath`.

Если asset отсутствует:

- в Editor он создаётся автоматически в `Assets/PRUnitySDK/Resources/PRUnitySDK`;
- в player build записывается ошибка и возвращается `null`.

После автоматического создания вызывается `SetDefaultSettings()` и method hooks этапа
`Initializing`.

Проектные настройки включают:

- release type;
- уровень debug-логов;
- стратегию resolver;
- интервал `PRMonoBehaviourHost` tick;
- physics debug;
- способ объединения time scale.

## Instantiate facade

`PRUnitySDK.Instantiate()` повторяет основные перегрузки `UnityEngine.Object.Instantiate`:

```csharp
Enemy enemy = PRUnitySDK.Instantiate(prefab, position, rotation, parent);
```

Сейчас facade напрямую делегирует Unity и не добавляет pooling или дополнительную
регистрацию. Он оставляет единую точку расширения на будущее.

## DataContainer

`DataContainer.Initialize<T>()` оборачивает инициализацию в диагностику SDK:

- предотвращает повторную инициализацию типа;
- отдельно сообщает об отсутствующем обязательном Resources-asset;
- отдельно сообщает о нескольких найденных assets;
- повторно выбрасывает исключение после логирования.

## Расширение SDK

Для модулей, которые должны автоматически подключаться, используйте method hooks
соответствующего этапа. Перед добавлением нового глобального свойства рассмотрите
регистрацию интерфейса в service resolver — это упрощает замену реализации.

## Известные ограничения

- `ScriptableObjectSingleton` содержит Editor API в runtime-файле под `#if UNITY_EDITOR`.
- Автоматический installer пока не создаёт assets, слои и теги.
- Выбранная в настройках `ResolveStrategy` сейчас не меняет поведение `ResolveService`:
  альтернативная ветка закомментирована.
- Инициализация глобальна и не предоставляет штатного полного reset между Play-сессиями
  без domain reload.

