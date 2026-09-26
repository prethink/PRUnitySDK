# Флаги проекта (RemoteFlags)

Значения по имени, которые площадка может поменять без пересборки игры: скорость, цена,
вариант A/B-теста. У Яндекса это «флаги» в консоли разработчика.

Не путать с `Core/FlagsSystem`: там решения «можно/нельзя» от компонентов, здесь —
настройки «имя → значение».

## Чтение

```csharp
if (PRUnitySDK.RemoteFlags.TryGetInt("intType", out int intType))
    ApplyType(intType);

float speed = PRUnitySDK.RemoteFlags.GetFloat("speed", 1f);
bool newShop = PRUnitySDK.RemoteFlags.GetBool("new_shop");
string variant = PRUnitySDK.RemoteFlags.GetString("variant", "A");
Difficulty difficulty = PRUnitySDK.RemoteFlags.GetEnum("difficulty", Difficulty.Normal);
```

| Метод | Что понимает |
| --- | --- |
| `TryGetString` / `GetString` | как есть |
| `TryGetInt` / `GetInt` | целое, инвариантная культура |
| `TryGetFloat` / `GetFloat` | дробное через точку или запятую |
| `TryGetBool` / `GetBool` | `true/false`, `1/0`, `yes/no`, `on/off` |
| `GetEnum<T>` | имя значения перечисления, без учёта регистра |

`Get…` с запасным значением возвращает его, если флага нет или он не разобрался.

Флаги площадки приходят при её запуске, до готовности SDK: читайте их после
`PRUnitySDK.ReadySignal`, а не в `Awake`.

## Откуда значения

| Реализация | Когда | Значения |
| --- | --- | --- |
| `LocalRemoteFlags` | без площадки: редактор, другие платформы, проект без интеграции | `PRSDKSettings → Remote Flags` |
| `YandexRemoteFlags` (`YG2.Integration`) | подключён YG2 | флаги Яндекса; флага нет — значение из настроек |

Настройки проекта — это значения по умолчанию: консоль задаёт только то, что меняют.
В редакторе YG2 отдаёт флаги из своих настроек (`InfoYG → Flags`).

Реализация выбирается как у других сервисов SDK: интеграция площадки подменяет свойство
`PRUnitySDK.RemoteFlags` через `[OverrideProperty(typeof(IRemoteFlags), …)]`, без неё
остаётся `LocalRemoteFlags`. Своя площадка — своя реализация `IRemoteFlags` тем же способом.
