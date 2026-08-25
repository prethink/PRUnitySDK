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

Для чтения доступны пары `TryGet*`/`Get*` для каждого поддерживаемого типа.

### fallback

У каждого `Get*` есть необязательный параметр `fallback` — значение, которое вернётся, если ключ ещё ни разу не сохранялся. По умолчанию он равен `default(T)`, поэтому вызовы без него работают как раньше.

```csharp
long coins = properties.GetLong("Coins");            // 0, если ключа нет
long start = properties.GetLong("Coins", 100);       // 100, если ключа нет

bool music = properties.GetBool("Music", true);      // по умолчанию включено
string nick = properties.GetString("Nick", "guest");
float volume = properties.GetFloat("Volume", 0.5f);
```

`fallback` подставляется **только при отсутствии ключа**. Сохранённое значение возвращается как есть — в том числе явно сохранённые `0`, `false` и `null`.

Когда нужно именно отличить «ключа нет» от «сохранён 0/false/null», по-прежнему используйте `TryGet*`: `fallback` этой разницы не покажет, если совпадёт с сохранённым значением.

## Enumeration и типизированные ключи

Legacy-перегрузки принимают `Enumeration` и используют его `Value` как строковый ключ. Для нового API предпочтительнее `EnumerationType<T>`: тип значения закреплён в самом ключе.

```csharp
public static readonly EnumerationType<bool> ShowHints = new(nameof(ShowHints));

properties.SetValue(ShowHints, true);
bool showHints = properties.GetValue(ShowHints, fallback: false);
properties.RemoveProperty(ShowHints);
```

Параметр `fallback` есть у всех трёх видов ключа:

```csharp
Enumeration coinsKey = Enumeration.GetOrCreate("Coins");
EnumerationType<long> typedCoinsKey = new("Coins");

long a = properties.GetValue("Coins", 100L);        // строковый ключ
long b = properties.GetValue(coinsKey, 100L);       // Enumeration
long c = properties.GetValue(typedCoinsKey, 100L);  // EnumerationType<long>
```

Для `EnumerationType<long>` и `EnumerationType<float>` есть `AddLong` и `AddFloat`.

## Сохранение

У методов записи и удаления два флага:

| Параметр | Текущее поведение |
| --- | --- |
| `save` | при `true` вызывает `GameManager.Instance.SaveProjectData()` |
| `requiredNotify` | при `true` публикует событие изменения в `EventBus` (см. [Уведомления](#уведомления)) |

Если нужно выполнить несколько изменений, передайте `save: false`, затем сохраните один раз:

```csharp
properties.SetLong("Coins", 100, save: false);
properties.SetBool("TutorialDone", true, save: false);
PRUnitySDK.Managers.Game.SaveProjectData();
```

Удаление отсутствующего ключа ничего не сохраняет. `RemoveProperty(string, Type, ...)` пишет warning для неподдерживаемого типа, а generic API выбрасывает `NotSupportedException`.

## Уведомления

При `requiredNotify: true` изменение свойства публикуется в [EventBus](../../@Events/EventBus/README.md) на двух уровнях — подписчик выбирает тот, который ему удобнее.

**Уровень 1 — только имя свойства.** Не зависит от типа и не требует приведений: подписчик сам решает, нужно ли ему читать значение.

```csharp
public class SaveIndicator : MonoBehaviour, IProjectPropertyChangedEvent
{
    public void OnProjectPropertyChanged(string propertyName)
    {
        // сюда приходят изменения свойств любого типа
    }
}
```

**Уровень 2 — готовые значения нужного типа.** По одному интерфейсу на поддерживаемый тип; один класс может реализовать сразу несколько. Вместе с новым приходит предыдущее значение — менеджер всё равно читает его перед записью, поэтому подписчику не нужно хранить свою копию, чтобы посчитать разницу.

```csharp
public class CoinsView : MonoBehaviour, ILongProjectPropertyChangedEvent
{
    public void OnLongProjectPropertyChanged(string propertyName, long previousValue, long currentValue)
    {
        if (propertyName != "Coins")
            return;

        label.text = currentValue.ToString();
        PlayGainAnimation(currentValue - previousValue);
    }
}
```

| Интерфейс | Метод |
| --- | --- |
| `IProjectPropertyChangedEvent` | `OnProjectPropertyChanged(string)` |
| `ILongProjectPropertyChangedEvent` | `OnLongProjectPropertyChanged(string, long previous, long current)` |
| `IFloatProjectPropertyChangedEvent` | `OnFloatProjectPropertyChanged(string, float previous, float current)` |
| `IBoolProjectPropertyChangedEvent` | `OnBoolProjectPropertyChanged(string, bool previous, bool current)` |
| `IStringProjectPropertyChangedEvent` | `OnStringProjectPropertyChanged(string, string previous, string current)` |
| `IDateTimeProjectPropertyChangedEvent` | `OnDateTimeProjectPropertyChanged(string, DateTime previous, DateTime current)` |
| `IProjectPropertyRemovedEvent` | `OnProjectPropertyRemoved(string, Type)` |

Порядок и условия рассылки:

- сначала уведомляются типизированные подписчики, затем общие;
- событие уходит **после** сохранения, поэтому подписчик видит уже зафиксированное состояние;
- если значение не изменилось (сравнение через `EqualityComparer<T>.Default`), уведомления не будет — `SetLong("Coins", 100)` подряд дважды разошлёт событие один раз;
- удаление несуществующего ключа события не публикует.

Фильтра по имени свойства на стороне шины нет: подписчик получает изменения всех свойств своего типа и сам сравнивает `propertyName`.

## Публичный API

| Группа | Методы |
| --- | --- |
| запись | `SetDateTime`, `SetLong`, `SetString`, `SetFloat`, `SetBool`, `SetValue<T>` |
| накопление | `AddLong`, `AddFloat` |
| безопасное чтение | `TryGetDateTime`, `TryGetLong`, `TryGetString`, `TryGetFloat`, `TryGetBool`, `TryGetValue<T>` |
| чтение с fallback | `GetDateTime`, `GetLong`, `GetString`, `GetFloat`, `GetBool`, `GetValue<T>` — все с необязательным `fallback` |
| удаление | `RemoveProperty(string, Type)`, `RemoveProperty<T>(string)`, `RemoveProperty<T>(EnumerationType<T>)` |

## Ограничения

- Доступ требует загруженного `ProjectData`; до `GameManager.ReadySignal` чтение и запись завершатся исключением.
- Один строковый ключ может одновременно существовать в словарях разных типов. Тип является частью фактической идентичности свойства.
- `ObjectProperties` присутствует в модели данных, но менеджер не предоставляет для него публичный API.
- Уведомления не батчатся: серия `Set*` с `save: false` разошлёт столько событий, сколько было изменений. Если это критично, отключайте рассылку через `requiredNotify: false` и уведомляйте подписчиков самостоятельно после последнего изменения.
- При первом сохранении свойства `previousValue` равен `default(T)`, и этот случай неотличим от ранее сохранённого `0`, `false` или `null`. Когда важно именно «свойства ещё не было», проверяйте через `TryGet*` до записи.
- Общее событие (`IProjectPropertyChangedEvent`) и удаление значений не несут — только имя, а для удаления ещё и тип.
