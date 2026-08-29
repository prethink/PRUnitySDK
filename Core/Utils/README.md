# PRUnitySDK Utils

Небольшие вспомогательные классы SDK. Утилиты времени используют `PRTime`, поэтому их следует создавать и вызывать после инициализации SDK.

`NameService` доступен через `PRUnitySDK.Utils.NameService`, но документируется как
общий runtime-сервис: [NameService и другие сервисы](../Services/README.md).

## Cooldown

`CooldownGameTime` измеряет интервал в игровом времени и останавливается вместе с игрой. `CooldownRealTime` использует реальное время SDK.

Новый cooldown готов к выполнению сразу:

```csharp
private readonly CooldownBase attackCooldown = new CooldownGameTime();

public bool TryAttack()
{
    return attackCooldown.TryExecute(0.5f, Attack);
}
```

Диагностическое сообщение о неготовности по умолчанию выключено. Его можно включить явно и при необходимости указать источник сообщения:

```csharp
private CooldownBase saveCooldown;

private void Awake()
{
    saveCooldown = new CooldownRealTime
    {
        LogNotReady = true,
        LogInitiator = this
    };
}
```

Если `LogInitiator` не задан, источником сообщения будет сам экземпляр cooldown.

Вариант с результатом возвращает `fallback`, если интервал ещё не прошёл:

```csharp
var target = searchCooldown.ExecuteWithResult(
    interval: 0.2f,
    action: FindTarget,
    fallback: previousTarget);
```

`ExecuteAfter` только запускает отложенное действие через coroutine host. Он не проверяет и не перезапускает интервал cooldown; для ограничения частоты следует использовать `TryExecute` или `ExecuteWithResult`.

Отрицательный интервал считается нулевым. Время считывается один раз на одну проверку, поэтому результат не меняется внутри вызова.

## Timer

`GameTimer` отсчитывает игровые секунды и учитывает логическую паузу и масштаб игрового времени SDK. `RealTimer` работает по реальным секундам SDK.

Таймер создаётся остановленным. `Start()` запускает или продолжает отсчёт, `Stop()` приостанавливает его, а `Reset()` останавливает и восстанавливает первоначальную длительность:

```csharp
private GameTimer roundTimer;

private void StartRound()
{
    roundTimer = new GameTimer(30);
    roundTimer.OnTick += UpdateTimerView;
    roundTimer.RegisterEndAction(EndRound);
    roundTimer.Start();
}
```

`CurrentTime` содержит оставшееся количество секунд. `OnTick` вызывается после уменьшения времени; при достижении нуля сначала вызывается `OnTick`, а затем действие завершения.

Таймер подписывается на `EventBus` уже в конструкторе, поэтому его обязательно нужно освобождать, даже если `Start()` не вызывался:

```csharp
private void OnDestroy()
{
    roundTimer?.Dispose();
}
```

После `Dispose()` экземпляр не следует запускать повторно. `End()` завершает отсчёт немедленно, тогда как `Stop()` только приостанавливает его.

## LayerMaskUtils

Маска слоёв в коде выглядит обычным числом, и по вызову `Physics.OverlapSphere(point,
radius, ~0)` не понять, намеренно там «все слои» или забытая заглушка. `LayerMaskUtils`
даёт таким значениям имена и проверяет сборку маски.

```csharp
int mask = LayerMaskUtils.Create("Entity", "Player", "Enemy");
mask = LayerMaskUtils.Remove(mask, gameObject.layer);   // не задевать самого себя

if (LayerMaskUtils.Contains(mask, hit.collider))
    Damage(hit.collider);
```

`Create` отличается от `LayerMask.GetMask` тем, что сообщает о слое, которого нет
в проекте. Стандартный метод в таком случае молча возвращает ноль, и физика перестаёт
находить что-либо вообще — без единой ошибки в консоли, поэтому искать причину
приходится долго.

| Метод | Что делает |
| --- | --- |
| `GetAnyLayer()` / `GetNoneLayer()` | все слои / ни одного |
| `IsEmpty(mask)` | маска пуста — проверка по ней ничего не найдёт |
| `ContainsLayer(mask, layer)` | слой входит в маску |
| `Contains(mask, GameObject)` / `Contains(mask, Component)` | объект лежит на слое из маски |
| `Create(params string[])` | собирает маску из имён с проверкой |
| `Add(mask, layer)` / `Remove(mask, layer)` | добавить или убрать слой |
| `Combine(params int[])` / `Exclude(mask, other)` | объединить маски или вычесть одну из другой |
| `GetLayers(mask)` | слои маски по номерам |
| `Describe(mask)` | имена слоёв через запятую — для логов и подписей |

`Describe` для полной маски отвечает «все слои»: перечислять 32 номера, из которых
большинство безымянные, бесполезно.
