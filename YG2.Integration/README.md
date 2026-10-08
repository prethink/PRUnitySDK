# Интеграция с YG2

Связывает PRUnitySDK с плагином YG2 (Яндекс Игры): запуск по готовности площадки, устройство,
время площадки, метрика, облачное сохранение, флаги, язык, имя игрока и курсор после паузы площадки.
Реклама, покупки, рейтинг и призраки на YG2 лежат в `PRUnitySDKPrivate/YG2.Integration` и относятся
к этому же модулю.

## Зависимости

- PRUnitySDK.Core
- [YG2 Plugin](https://max-games.ru/plugin-yg/) в `Assets/PluginYourGames`

| Раздел | Что подменяет | Модуль YG2 |
| --- | --- | --- |
| `Bootstrap.YGPlugin` | SDK собирается после `YG2.onGetSDKData` | ядро плагина |
| `DeviceInfo` | `DeviceInfoBase` | EnvirData |
| `ServerTime` | `IServerTime` | ServerTime |
| `Metrics` | `MetricBase` | Metrica |
| `GameDataStorage` | `IGameDataStorage` | Storage |
| `RemoteFlags` | `IRemoteFlags`, см. `Core/RemoteFlags/README.md` | Flags |
| `Translate` | `ILanguageManager` | Localization |
| `PlayerNameService` | `PlayerNameServiceBase` | Authorization |
| `Cursor` | возвращает курсор `CursorManager` после паузы площадки | ядро плагина |
| `Payments` | валюта площадки `Yan` | — |

## Как использовать

Адаптеры подключаются сами атрибутом `[OverrideProperty(..., PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]`.
Код игры обращается к `PRUnitySDK.ServerTime`, `PRUnitySDK.Metric` и остальным сервисам, не зная о площадке.

## Как отключить

Окно *PRUnitySDK → Модули*, вкладка «Интеграции», слот «Площадка»: выбрать «Не подключено» и нажать
«Применить». В Player Settings появится символ `PRSDK_DISABLE_YG2`, и код обеих папок `YG2.Integration`
из сборки уйдёт. Там же показано, какие модули плагина установлены.

Без интеграции:

| Сервис | Что работает |
| --- | --- |
| запуск | SDK собирается сразу в `Bootstrap.Awake` |
| время | `LocalServerTime` |
| метрика | `DummyMetric` |
| сохранение | `PlayerPrefsSaveLoadManager` |
| флаги | `LocalRemoteFlags` |
| язык | `LanguageManager` |
| имя игрока, устройство | `LocalPlayerNameService`, `LocalDeviceInfo` |

Сам плагин при отключённой интеграции можно оставить в проекте или удалить: проект собирается в обоих
случаях. Без плагина интеграцию нужно держать отключённой. Каждый новый `.cs` в этой папке оборачивается
в `#if !PRSDK_DISABLE_YG2`, подробности — `Core/ModuleSystem/README.md`.
