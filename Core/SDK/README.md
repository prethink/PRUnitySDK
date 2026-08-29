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
8. Регистрация фоновых задач, помеченных `[AutoBackgroundTask]`.
9. Установка `IsInitialized`.
10. Публикация `ISDKEvents.OnInitialized()`.
11. Перевод `ReadySignal` в готовое состояние.

Фоновые задачи регистрируются до `IsInitialized`, но выполняться начинают только после
него: трекер сверяется с состоянием SDK на каждом проходе, поэтому первый запуск
приходится на полностью готовый проект. См.
[BackgroundTasks](../BackgroundTasks/README.md).

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

Успешные операции сохраняются в `PRUnitySDK.InitializationHistory` в порядке запуска.
Каждая запись содержит категорию (`Module`, `Manager`, `Singleton`, `Factory`,
`MonoWindow`, `Notifier` или обычный `Type`), имя, тип контракта, фактический тип реализации и полное время операции в
миллисекундах. Общие точки
`InitializeModuleSDK`, `InitializeManager`, `InitializeType`, инициализация core-singleton
и generic `RegisterFactory` добавляют записи автоматически. Caller возвращает созданный
экземпляр, поэтому категория и фактическая реализация не указываются вручную. Данные отображаются на вкладке
`Initialization` окна `PRUnitySDK/Windows/Debug Window` в Play Mode.

`MonoWindowFactoryBase` и `NotifierFactoryBase` автоматически измеряют только первое
фактическое создание singleton-экземпляра; возврат уже созданного объекта повторную запись
не добавляет.

Например, при Yandex-интеграции модуль хранилища отображается с контрактом
`IGameDataStorage` и реализацией `YandexGameDataStorager`.

## ResourcePaths

Канонические пути для `Resources.Load`, правила их использования и расширения модулями
описаны в отдельной [документации ResourcePaths](../ResourcePaths/README.md).

## Настройки и база данных

`PRSDKSettings` и `PRSDKDatabase` наследуются от `ScriptableObjectSingleton<T>`. Ассет
ищется в два шага:

1. у активного проекта (`PRSDKProject` через указатель `PRSDKActiveProject`);
2. если проект не выбран или этой части в нём нет — в `Resources` по
   `PRUnitySDK.ResourcePaths.CorePath`, как было раньше.

Второй шаг оставлен намеренно: игра, не переходившая на проекты, продолжает работать
без правок. Подробности о проектах — в [PRUnityData/README.md](../../../PRUnityData/README.md).

Если asset отсутствует:

- в Editor он создаётся автоматически в `Assets/PRUnityData`;
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

Подробное описание `MethodHook`, стадий и механизма замены сервисов находится в
[документации Attributes](../@Attributes/README.md).

## Известные ограничения

- `ScriptableObjectSingleton` содержит Editor API в runtime-файле под `#if UNITY_EDITOR`.
- Автоматический installer пока не создаёт assets, слои и теги.
- Выбранная в настройках `ResolveStrategy` сейчас не меняет поведение `ResolveService`:
  альтернативная ветка закомментирована.
- Инициализация глобальна и не предоставляет штатного полного reset между Play-сессиями
  без domain reload.
