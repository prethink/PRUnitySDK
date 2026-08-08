# Coroutines

Набор объектных обёрток над Unity-корутинами. Они предоставляют единые методы запуска,
перезапуска и остановки, умеют выполняться на заданном `MonoBehaviour` или на глобальном
`PRMonoBehaviourHost` и используют правила паузы PRUnitySDK.

## Основные типы

| Тип | Поведение |
| --- | --- |
| `PRCoroutineBase` | Общий lifecycle корутины: `Execute`, `StopAndExecute`, `Stop` |
| `WaitSecondsCoroutineBase` | Основа одноразовых задержек |
| `WaitGameSecondsCoroutine` | Задержка по `PRTime.GameDeltaTime` |
| `WaitRealSecondsCoroutine` | Задержка по `PRTime.RealDeltaTime` |
| `UnityYieldCoroutineBase<T>` | Бесконечный callback после Unity yield-инструкции |
| `LateFixedUpdateCoroutine` | Callback после каждого `WaitForFixedUpdate` |
| `WaitForEndOfFrameCoroutine` | Callback после каждого `WaitForEndOfFrame` |
| `CanvasGroupFadeCoroutine` | Ожидание и последующее затухание CanvasGroup |

## Владелец корутины

Если в конструктор передан `MonoBehaviour`, Unity запускает корутину на нём. При
уничтожении или отключении соответствующего GameObject применяются стандартные правила
Unity для остановки корутин.

Если владелец не передан, используется `PRMonoBehaviourHost.Instance`. Такая корутина
не привязана к объекту, который создал обёртку, поэтому её рекомендуется явно остановить.

## Запуск и остановка

```csharp
private WaitGameSecondsCoroutine delay;

private void StartDelay()
{
    delay ??= new WaitGameSecondsCoroutine(OnCompleted, 2f, this);
    delay.StopAndExecute();
}

private void OnCompleted()
{
    Debug.Log("Delay completed");
}

private void OnDisable()
{
    delay?.Stop();
}
```

- `Execute()` запускает новый экземпляр, не останавливая предыдущий.
- `StopAndExecute()` останавливает последний сохранённый запуск и запускает новый.
- `Stop()` возвращает `false`, если корутина ещё не запускалась.

## Игровое и реальное время

```csharp
new WaitGameSecondsCoroutine(callback, 1f, this).Execute();
new WaitRealSecondsCoroutine(callback, 1f, this).Execute();
```

Обе реализации ожидают завершения логической паузы через `WaitPause`. Разница состоит
в источнике delta time после продолжения игры:

- `GameDeltaTime` учитывает глобальный слой `PRTimeScale`;
- `RealDeltaTime` не применяет игровой time scale.

## Периодические корутины

`LateFixedUpdateCoroutine` и `WaitForEndOfFrameCoroutine` работают бесконечно:

```csharp
private LateFixedUpdateCoroutine lateFixedUpdate;

private void OnEnable()
{
    lateFixedUpdate = new LateFixedUpdateCoroutine(AfterPhysics, this);
    lateFixedUpdate.Execute();
}

private void OnDisable()
{
    lateFixedUpdate?.Stop();
}
```

Callback'и хранятся в `HashSet<Action>`, поэтому одинаковый экземпляр делегата не
добавляется повторно.

## Создание собственной корутины

```csharp
public class WaitUntilReadyCoroutine : PRCoroutineBase
{
    private readonly System.Action callback;

    public WaitUntilReadyCoroutine(System.Action callback, MonoBehaviour owner)
        : base(owner)
    {
        this.callback = callback;
    }

    protected override IEnumerator InternalExecute()
    {
        yield return new WaitUntil(() => PRUnitySDK.ReadySignal.IsReady);
        callback?.Invoke();
    }
}
```

## Ограничения

- `CurrentCoroutine` хранит только последний запуск. Несколько вызовов `Execute()` нельзя
  затем остановить одним вызовом `Stop()`.
- `Stop()` не очищает `CurrentCoroutine`; повторная остановка всё равно вернёт `true`.
- Для бесконечных корутин без владельца ответственность за остановку лежит на вызывающем коде.

