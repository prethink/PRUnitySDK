# PRTime

`PRTime` — централизованный источник времени PRUnitySDK. Он отделяет реальное время
кадра от игрового времени, применяет темп мира `PRTimeScale`, учитывает логическую
паузу и публикует временные события через `EventBus`.

## Значения времени

| Свойство | Описание |
| --- | --- |
| `RealTime` | Накопленное время без PR time scale, но с учётом логической паузы |
| `GameTime` | Накопленное время в темпе мира (`TimeLayer.World`) и с паузой |
| `RealDeltaTime` | Delta текущего кадра без PR time scale |
| `GameDeltaTime` | `RealDeltaTime`, умноженный на темп мира |
| `RealFixedDeltaTime` | Базовый `Time.fixedDeltaTime` |
| `GameFixedDeltaTime` | Fixed delta в темпе мира |
| `CurrentRealSecond` | Полная секунда `RealTime` |
| `CurrentGameSecond` | Полная секунда `GameTime` |
| `LastRawTime` | Последнее значение `Time.realtimeSinceStartup` |

Темп мира читается через `PRTimeScale.GetWorldTimeScale()`. Замедление «всего, кроме игрока» замедляет и
игровое время: объекту в своём темпе нужен `PRTimeScale.Resolve(слои)` или `RelativeToWorld(слои)`.

Пока SDK загружается, время не идёт: первый кадр после загрузки не засчитывает её себе.

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

Событие публикуется на каждую прошедшую секунду: если за кадр прошло три (подвисание, сильное ускорение
модификаторами `PRTimeScale` или для проверки), обработчик получит три вызова подряд с номерами по порядку.
Обработчики, которые считают «событие — одна секунда» (доход StealEgg, `Timer`, метрики), поэтому секунд не теряют.

## Ускорение для проверки

`PRDebugTime.Speed` (1–20) — вся игра идёт в N раз быстрее, как тестируют idle-игры: шаг `PRTime` (игровое и реальное
время, секундные события) умножается на множитель, `Time.timeScale` тоже (движение, физика, анимации, боты), а часы
таймеров `PRUnitySDK.ServerTime` уходят вперёд на `PRDebugTime.Offset` (обёртка `PRDebugServerTime`) — созревание,
перезарядки, подарки, офлайн-доход. Звук и музыка не ускоряются.

Регулируется в окне отладки: вкладка Time, раздел «Скорость времени» (ползунок, кнопки x1…x20, сдвиг часов и его
сброс). Только редактор и отладочная сборка; каждый запуск — с x1, после выхода из Play `Time.timeScale` снова 1.
Сдвиг часов не сохраняется: таймеры, поставленные при ускорении, после перезапуска покажутся длиннее; по той же
причине кнопка сброса сдвига спрашивает подтверждение.

Шаг `PRTime` за кадр не больше, чем у физики: Unity продвигает её не дальше `Time.maximumDeltaTime` (0,333 с) за кадр.
Иначе при низком FPS доход и таймеры обгоняли бы движение, а секундные события терялись бы (событие одно на кадр).
Поэтому x20 честно идёт при FPS от 60, при 30 FPS реальное ускорение около x10 — для всего сразу. На подвисании
кадра дольше 0,333 с игра при ускорении, как и физика, проживает только 0,333 с.

Временные модификаторы `PRTimeScale` (хитстоп, слоу-мо) отсчитывают срок по `PRTime.RealTime` и при ускорении
заканчиваются в N раз быстрее — как и всё остальное в игре: x10 выглядит как запись игры, прокрученная в 10 раз быстрее.

## Сброс

```csharp
PRTime.Instance.Reset();
```

`Reset()` обнуляет накопленное время и delta, а `LastRawTime` синхронизирует с
`Time.realtimeSinceStartup`. Счётчики последних секунд не сбрасываются явно, поэтому
после ручного reset возможны дополнительные события смены секунды.

## Инициализация

`PRTime` создаётся как `PRMonoBehaviourSingletonBase<PRTime>`. Инициализация SDK должна
завершиться до штатного обновления времени: до неё `Update` время не двигает.

Время считается в обычных Unity `Update`/`FixedUpdate`, а не в `PRUpdate` через раннер: на паузе
`PRTime` тоже работает — обнуляет дельты. `[DefaultExecutionOrder(-1000)]` ставит его раньше
`PRMonoBehaviourHost`, поэтому все, кого раннер вызывает в этом кадре, читают уже посчитанное время.

Смена паузы (`OnPauseStateChanged`) начинает отсчёт кадра заново. Пока окно свёрнуто или вкладка скрыта,
кадров нет, и без этого первый кадр после возвращения посчитал бы шагом весь простой. Отдельно слушать
фокус не нужно: `GameManager` переводит его в паузу SDK. При наличии второго экземпляра его GameObject
уничтожается в `Awake`.

## Связанные системы

- [PRTimeScale](../PRTimeScale/README.md)
- [PauseSystem](../PauseSystem/README.md)
- [Coroutines](../Coroutines/README.md)
- [Yields](../Yields/README.md)
