# Интеграция с GamePush

Связывает PRUnitySDK с плагином [GamePush](https://github.com/GamePushService/GamePush-Unity-plugin):
запуск по готовности площадки, устройство, время сервера, метрика, облачное сохранение, переменные,
язык, имя игрока и оценка игры. Реклама, покупки и рейтинг на GamePush лежат
в `PRUnitySDKPrivate/GamePush.Integration` и относятся к этому же модулю.

Это вторая площадка в слоте «Площадка», рядом с `YG2.Integration`. Включается одна из них.

## Зависимости

- PRUnitySDK.Core
- плагин GamePush в `Assets/Plugins/GamePush`

| Раздел | Что подменяет | Плагин GamePush |
| --- | --- | --- |
| `Bootstrap.GamePush` | SDK собирается после `GP_Init.OnReady` (не поднялся — после `OnError`, без площадки); о готовности игры (`GP_Game.GameReady`) площадке сообщается, когда убран экран загрузки | `GP_Init`, `GP_Game` |
| `DeviceInfo` | `DeviceInfoBase`. Планшета и телевизора GamePush не различает | `GP_Device` |
| `ServerTime` | `IServerTime` | `GP_Server` |
| `Metrics` | `MetricBase`: цели `GP_Analytics.Goal`, параметры — одной строкой JSON | `GP_Analytics` |
| `GameDataStorage` | `IGameDataStorage`: всё сохранение строкой в поле игрока `prsave` | `GP_Player` |
| `RemoteFlags` | `IRemoteFlags`: переменные GamePush, запас — настройки проекта | `GP_Variables` |
| `Translate` | `ILanguageManager` | `GP_Language` |
| `PlayerNameService` | `PlayerNameServiceBase` | `GP_Player` |
| `Review` | `IReviewService` | `GP_App` |

Аналогов `Cursor` и `Focus` из YG2 нет: GamePush курсор не трогает, а фокус после рекламы и оплаты
возвращает сам (`focusGame()` в его `_gpUnity.jspre`).

## Настройка площадки

1. В панели GamePush, раздел «Игроки», завести строковое поле `prsave`
   (`GamePushGameDataStorager.SaveFieldKey`). В поле, которого нет в панели, сохранение не ляжет.
   Всё сохранение игрока у GamePush — не больше 1 МБ.
2. Для каждой таблицы рейтинга — числовое поле игрока с тем же ключом, что имя таблицы
   в настройках рейтинга (`PRSDKSettings → Rating`).
3. Продукты в каталоге проекта (`PurchaseCatalogSettings`) указываются тегом продукта GamePush.
4. В настройках плагина выключить автопаузу на рекламе (`AUTO_PAUSE_ON_ADS`): она пишет
   `Time.timeScale` и звук в обход менеджера паузы SDK, а паузу на время рекламы уже ставит
   `AdManagerBase`.

## Как использовать

Адаптеры подключаются сами атрибутом `[OverrideProperty(..., PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]`.
Код игры обращается к `PRUnitySDK.ServerTime`, `PRUnitySDK.Metric` и остальным сервисам, не зная о площадке.
Сервисы, которые обращаются к площадке, сначала проверяют `GP_Init.isReady`: в браузере обращение
к неподнятому GamePush падает в JavaScript, и поймать это из C# нельзя.

## Как включить и отключить

Окно *PRUnitySDK → Модули*, вкладка «Интеграции», слот «Площадка»: выбрать «GamePush» или другую площадку
и нажать «Применить». Окно:

- пишет символ `PRSDK_DISABLE_GAMEPUSH` (или `PRSDK_DISABLE_YG2`) отключённой площадке — её код уходит из сборки;
- прячет папку плагина отключённой площадки, переименовывая её в `GamePush~` (или `PluginYourGames~`).
  Unity такие папки не видит, поэтому чужие `.jslib`, `.jspre`, автозапуск и обработчики сборки
  в страницу не попадают. Папка плагина включённой площадки возвращается на место;
- ставит шаблон WebGL включённой площадки: `PROJECT:GamePush` или `PROJECT:YandexGames`.

За это отвечают поля манифеста `Hide Plugin When Disabled` и `Web GL Template`.

Без включённой площадки SDK работает на реализациях по умолчанию, как описано в `YG2.Integration/README.md`.
Каждый новый `.cs` в этой папке оборачивается в `#if !PRSDK_DISABLE_GAMEPUSH`, подробности —
`Core/ModuleSystem/README.md`.
