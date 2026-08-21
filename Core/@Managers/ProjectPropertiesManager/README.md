# ProjectPropertiesManager

`ProjectPropertiesManager` предоставляет типизированный доступ к произвольным значениям внутри `ProjectData.ProjectProperties`. Поддерживаются `long`, `float`, `DateTime`, `string` и `bool`; каждый тип хранится в отдельном словаре.

После готовности данных менеджер доступен через:

```csharp
ProjectPropertiesManager properties = PRUnitySDK.Managers.ProjectProperties;
```

## Строковые ключи

```csharp
properties.SetLong("Coins", 100);
properties.AddLong("Coins", 25);

if (properties.TryGetLong("Coins", out long coins))
{
    // значение было сохранено
}

float playTime = properties.GetFloat("PlayTime"); // 0, если ключ отсутствует
properties.RemoveProperty<long>("Coins");
```

Для чтения доступны пары `TryGet*`/`Get*` для каждого поддерживаемого типа. `Get*` возвращает `default(T)`, если ключ отсутствует; используйте `TryGet*`, когда нужно отличать отсутствие от сохранённого `0`, `false` или `null`.

## Enumeration и типизированные ключи

Legacy-перегрузки принимают `Enumeration` и используют его `Value` как строковый ключ. Для нового API предпочтительнее `EnumerationType<T>`: тип значения закреплён в самом ключе.

```csharp
public static readonly EnumerationType<bool> ShowHints = new(nameof(ShowHints));

properties.SetValue(ShowHints, true);
bool showHints = properties.GetValue(ShowHints, defaultValue: false);
properties.RemoveProperty(ShowHints);
```

Для `EnumerationType<long>` и `EnumerationType<float>` есть `AddLong` и `AddFloat`.

## Сохранение

У методов записи и удаления два флага:

| Параметр | Текущее поведение |
| --- | --- |
| `save` | при `true` вызывает `GameManager.Instance.SaveProjectData()` |
| `requiredNotify` | присутствует в публичном API, но текущая реализация не публикует уведомление |

Если нужно выполнить несколько изменений, передайте `save: false`, затем сохраните один раз:

```csharp
properties.SetLong("Coins", 100, save: false);
properties.SetBool("TutorialDone", true, save: false);
PRUnitySDK.Managers.Game.SaveProjectData();
```

Удаление отсутствующего ключа ничего не сохраняет. `RemoveProperty(string, Type, ...)` пишет warning для неподдерживаемого типа, а generic API выбрасывает `NotSupportedException`.

## Публичный API

| Группа | Методы |
| --- | --- |
| запись | `SetDateTime`, `SetLong`, `SetString`, `SetFloat`, `SetBool`, `SetValue<T>` |
| накопление | `AddLong`, `AddFloat` |
| безопасное чтение | `TryGetDateTime`, `TryGetLong`, `TryGetString`, `TryGetFloat`, `TryGetBool`, `TryGetValue<T>` |
| чтение с default | `GetDateTime`, `GetLong`, `GetString`, `GetFloat`, `GetBool`, `GetValue<T>` |
| удаление | `RemoveProperty(string, Type)`, `RemoveProperty<T>(string)`, `RemoveProperty<T>(EnumerationType<T>)` |

## Ограничения

- Доступ требует загруженного `ProjectData`; до `GameManager.ReadySignal` чтение и запись завершатся исключением.
- Один строковый ключ может одновременно существовать в словарях разных типов. Тип является частью фактической идентичности свойства.
- `ObjectProperties` присутствует в модели данных, но менеджер не предоставляет для него публичный API.
- Параметр `requiredNotify` пока зарезервирован и игнорируется; подписчики EventBus не получают событие изменения.
