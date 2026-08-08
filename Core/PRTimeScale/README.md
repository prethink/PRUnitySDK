# PRTimeScale

`PRTimeScale` управляет независимыми слоями скорости времени. Глобальный слой влияет на
весь игровой мир, а дополнительные слои позволяют отдельно замедлять игрока, NPC или UI.
Система не изменяет `UnityEngine.Time.timeScale` автоматически.

## Стандартные слои

`PRTimeScaleEnumerationProvider` объявляет:

- `Global`;
- `Player`;
- `NPC`;
- `UI`.

Провайдер включает унаследованные значения, поэтому проект может расширить список своей
реализацией `EnumerationProviderBase`.

## Инициализация

Во время `PRUnitySDK.InitializeSDK()` вызывается `PRTimeScale.SingletonInitialize()`.
Все известные слои получают значение `1f`. До инициализации методы глобального разрешения
возвращают `DefaultTimeScale`.

## Установка значений

```csharp
PRTimeScale.Instance.SetGlobalTimeScale(0.5f);
PRTimeScale.Instance.SetTimeScale(PRTimeScaleEnumerationProvider.Player, 0.8f);
```

После изменения публикуется `IOnPRTimeScaleChange`:

```csharp
public class TimeScaleListener : MonoBehaviour, IOnPRTimeScaleChange
{
    private void OnEnable() => EventBus.Subscribe(this);
    private void OnDisable() => EventBus.Unsubscribe(this);

    public void OnPRTimeScaleChange(Enumeration layer, float value)
    {
        Debug.Log($"{layer}: {value}");
    }
}
```

## Получение итогового масштаба

```csharp
float playerScale = PRTimeScale.Instance.Resolve(
    PRTimeScaleEnumerationProvider.Player);
```

Результат зависит от `PRUnitySDK.Settings.Project.TimeScaleCombineMode`:

| Режим | Результат |
| --- | --- |
| `Multiply` | `global * layer` |
| `Max` | Максимальное из global и layer |
| `Min` | Минимальное из global и layer |
| `OverrideGlobal` | Значение layer без global |

`Resolve()` без аргумента возвращает глобальный scale. Переданный слой должен быть
зарегистрирован при инициализации, иначе прямое обращение к словарю завершится исключением.

## Временное изменение

```csharp
PRTimeScale.Instance.SetTimeScaleTemporarily(
    PRTimeScaleEnumerationProvider.Player,
    value: 0.25f,
    callBackTime: 2f);
```

Система запоминает предыдущее значение и восстанавливает его после задержки. Пока для
слоя действует временная задача, повторный запрос для этого слоя игнорируется.

## Сброс

```csharp
PRTimeScale.Instance.Reset();
```

Все зарегистрированные слои возвращаются к `1f`, и для каждого публикуется событие.

## ITimeScaleLayer

Компонент может сообщить, какой слой влияет на него:

```csharp
public class NPCUnit : MonoBehaviour, ITimeScaleLayer
{
    public Enumeration GetTimeScaleLayer()
        => PRTimeScaleEnumerationProvider.NPC;
}
```

Затем потребитель получает итоговый scale через `Resolve(unit.GetTimeScaleLayer())`.

## Связь с PRTime

`PRTime.GameDeltaTime` и `GameFixedDeltaTime` автоматически применяют только глобальный
слой. Масштаб конкретного объекта нужно применять отдельно через `Resolve(layer)`.

