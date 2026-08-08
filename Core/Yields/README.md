# Yields

Папка содержит `CustomYieldInstruction`, связывающие Unity-корутины с логической паузой
PRUnitySDK.

## WaitPause

Удерживает корутину, пока активна логическая пауза:

```csharp
yield return WaitPause.Instance;
```

Эквивалентное условие:

```csharp
keepWaiting => PRUnitySDK.PauseManager.IsLogicPaused;
```

`WaitPause` не ждёт наступления паузы. Если игра уже продолжается, инструкция завершится
сразу. Класс предоставляет общий экземпляр `Instance`, поэтому не требует новых аллокаций.

Пример pause-aware цикла:

```csharp
while (remainingTime > 0f)
{
    yield return WaitPause.Instance;
    remainingTime -= PRTime.Instance.GameDeltaTime;
    yield return null;
}
```

## WaitContinueGame

Удерживает корутину, пока логическая пауза отсутствует, и завершается при её наступлении:

```csharp
yield return new WaitContinueGame();
Debug.Log("Logic pause started");
```

Эквивалентное условие:

```csharp
keepWaiting => !PRUnitySDK.PauseManager.IsLogicPaused;
```

Название может читаться неоднозначно: инструкция ожидает не продолжения игры, а момента,
когда текущая продолжающаяся игра будет поставлена на паузу.

## Выбор инструкции

| Задача | Инструкция |
| --- | --- |
| Приостановить выполнение вместе с игрой | `WaitPause.Instance` |
| Дождаться включения логической паузы | `new WaitContinueGame()` |

## Ограничения

- Обе инструкции требуют доступного `PRUnitySDK.PauseManager`.
- Они реагируют только на итоговый `IsLogicPaused`, а не на конкретную причину паузы.
- `WaitContinueGame` не имеет singleton-экземпляра и создаёт объект при каждом `new`.

