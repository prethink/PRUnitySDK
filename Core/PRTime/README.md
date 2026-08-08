# PRTime

`PRTime` — централизованный источник времени PRUnitySDK. Он отделяет реальное время
кадра от игрового времени, применяет глобальный `PRTimeScale`, учитывает логическую
паузу и публикует временные события через `EventBus`.

## Значения времени

| Свойство | Описание |
| --- | --- |
| `RealTime` | Накопленное время без PR time scale, но с учётом логической паузы |
| `GameTime` | Накопленное время с глобальным PR time scale и паузой |
| `RealDeltaTime` | Delta текущего кадра без PR time scale |
| `GameDeltaTime` | `RealDeltaTime`, умноженный на глобальный PR time scale |
| `RealFixedDeltaTime` | Базовый `Time.fixedDeltaTime` |
| `GameFixedDeltaTime` | Fixed delta с глобальным PR time scale |
| `CurrentRealSecond` | Полная секунда `RealTime` |
| `CurrentGameSecond` | Полная секунда `GameTime` |
| `LastRawTime` | Последнее значение `Time.realtimeSinceStartup` |

Термин `Real` означает «без PR time scale», но не «выполняется во время паузы».
При активной логической паузе обе delta time устанавливаются в `0`, а накопленные
`RealTime` и `GameTime` не увеличиваются.

## Использование

```csharp
protected override void PRUpdate()
{
    float delta = PRTime.Instance.GameDeltaTime;
    transform.position += velocity * delta;
}
```

Для логики, которая не должна учитывать замедление игрового времени:

```csharp
float delta = PRTime.Instance.RealDeltaTime;
```

Для физического расчёта:

```csharp
float fixedDelta = PRTime.Instance.GameFixedDeltaTime;
```

Не смешивайте `Time.deltaTime`, `PRTime.RealDeltaTime` и `PRTime.GameDeltaTime` внутри
одной системы без явной причины: при паузе или замедлении они ведут себя по-разному.

## События времени

`PRTime` публикует события:

- `IOnUpdateEvent` — после обычного Unity Update обработчика PRTime;
- `IOnPRUpdateEvent` — после обновления PR-времени;
- `IOnRealSecondsEvent` — при смене полной real-секунды;
- `IOnGameSecondsEvent` — при смене полной game-секунды.

Пример секундного игрового тика:

```csharp
public class IncomeTimer : MonoBehaviour, IOnGameSecondsEvent
{
    private void OnEnable() => EventBus.Subscribe(this);
    private void OnDisable() => EventBus.Unsubscribe(this);

    public void OnGameSecondTick(long second)
    {
        Debug.Log($"Game second: {second}");
    }
}
```

При большом скачке времени событие публикуется один раз с текущим номером секунды,
а не отдельно для каждой пропущенной секунды.

## Сброс

```csharp
PRTime.Instance.Reset();
```

`Reset()` обнуляет накопленное время и delta, а `LastRawTime` синхронизирует с
`Time.realtimeSinceStartup`. Счётчики последних секунд не сбрасываются явно, поэтому
после ручного reset возможны дополнительные события смены секунды.

## Инициализация

`PRTime` создаётся как `PRMonoBehaviourSingletonBase<PRTime>`. Инициализация SDK должна
завершиться до штатного обновления времени. При наличии второго экземпляра его GameObject
уничтожается в `Awake`.

## Связанные системы

- [PRTimeScale](../PRTimeScale/README.md)
- [PauseSystem](../PauseSystem/README.md)
- [Coroutines](../Coroutines/README.md)
- [Yields](../Yields/README.md)

