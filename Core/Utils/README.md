# PRUnitySDK Utils

Небольшие вспомогательные классы SDK. Утилиты времени используют `PRTime`, поэтому их следует создавать и вызывать после инициализации SDK.

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
