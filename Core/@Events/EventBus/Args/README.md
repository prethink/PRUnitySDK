# Аргументы событий

## EventArgsBase
EventArgsBase - Базовый класс аргументов событий. Содержит в себе дату события и EventId.

## GameplayEventArgsBase

События игрового процесса приходят в `IGameplayEvent.Track(args)`; вид события — тип аргументов.

### Игровые сигналы

`GameSignalEventArgs` — «случилось событие» без своих подробностей: ключ из
`GameSignalEnumerations` и необязательный источник (`IEntity`, например игрок). Набор пустой,
сигналы объявляет проект partial-классом рядом с тем, что их поднимает:

```csharp
public partial class GameSignalEnumerations
{
    public static readonly Enumeration LuckyBoxOpened = new(nameof(LuckyBoxOpened));
}

GameplayEvents.RaiseSignal(GameSignalEnumerations.LuckyBoxOpened, player);
```

Слушатель проверяет `args is GameSignalEventArgs signal && signal.Signal == ...`. Так ждёт
сигнала шаг обучения `TutorialEventStep`.

## UIEventArgsBase

## CommandEventArgsBase