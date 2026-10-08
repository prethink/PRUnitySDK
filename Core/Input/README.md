# Ввод

Слой ввода в SDK — **транспорт, а не биндинг**. Он хранит уже разобранное состояние
ввода и раздаёт его по владельцам, но не читает устройства и не знает, какие действия
есть в игре.

```mermaid
flowchart LR
    subgraph SRC["Источники — наследники PlayerInputSourceBase"]
        KB["Клавиатура и мышь"]
        JOY["Джойстик, кнопки UI"]
        BOT["ИИ бота"]
    end

    KB --> ST
    JOY --> ST
    BOT --> ST

    ST["PlayerInputState<br/>ключ → held / pressed /<br/>released / axis / vector"]
    ST --> TR["InputTranslator<br/>реестр состояний по InputGuid"]
    TR --> C1["Контроллер движения"]
    TR --> C2["Камера"]
    TR --> C3["Любой потребитель"]
```

SDK работает с любым ключом `Enumeration`. Конкретный набор, например `MoveVector` и `JumpAction`, игра объявляет наследником `EnumerationProviderBase`. Один translator обслуживает и живого игрока, и бота: бот пишет в состояние то же, что клавиатура.

## Состояние владельца

`PlayerInputState` — обычный C#-класс, привязанный к `InputGuid` владельца.

| Метод | Что делает |
| --- | --- |
| `IsHeld`, `IsPressed`, `IsReleased` | чтение состояния ключа |
| `GetAxis`, `GetVector` | чтение осевого и векторного ввода |
| `SetKey(key, isDown)` | обновление по состоянию устройства |
| `SetAxis`, `SetVector` | запись значений, поднимает события изменения |
| `FrameSync()` | перевод накопленного за кадр в снимок для чтения |

`SetKey` сам определяет переход: нажатие и отпускание выставляются только при смене
состояния, поэтому источник ввода может звать его каждый кадр и не следить
за предыдущим значением.

`FrameSync()` переводит накопленный за кадр ввод в снимок для чтения. Читатели видят стабильный снимок, пока накапливается следующий кадр.

## Источник ввода

Свой источник — наследник `PlayerInputSourceBase`. База ищет владельца и вызывает `InputHandle()` в фазе Update, до синхронизации кадра translator'ом. Устройство читает и ключи назначает наследник.

```csharp
public class GamepadInput : PlayerInputSourceBase
{
    [SerializeField] private PlayerBase target;

    protected override PlayerBase ResolveOwner() => target;

    protected override void InputHandle()
    {
        InputState.SetVector(GameInput.MoveVector,
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));

        InputState.SetKey(GameInput.JumpAction, Input.GetButton("Jump"));
    }
}
```

| Член | Для чего |
| --- | --- |
| `ResolveOwner()` | вернуть владельца или `null`, если он ещё не готов |
| `InputHandle()` | прочитать устройство и записать ключи |
| `InputState` | состояние ввода владельца |
| `Owner` | сам владелец, когда нужен не только ввод |
| `CanInput()` | добавить своё условие поверх базового |

Владельца база ищет каждый кадр, пока не найдёт, потому что источник на сцене обычно стартует раньше игрока. Свойство называется `InputState`, потому что `Input` занят `UnityEngine.Input`.

`KeyboardInputSourceBase` добавляет к этому одно: проверку `PRUnitySDK.DeviceInfo.IsDesktop()`
перед поиском владельца. На мобильной сборке такой источник не делает ничего.

Если источник создаётся из префаба, берите `PlayerInputSourceFactoryBase<T>` — он собирает
путь из `ResourcePaths.InputsPath` и имени префаба:

```csharp
public class GamepadInputFactory : PlayerInputSourceFactoryBase<GamepadInput>
{
    public override string Name => "GamepadInput";
}
```

## Кадровый цикл

Самое неочевидное в модуле — когда именно ввод становится виден. Удержание пишется
сразу, а нажатие и отпускание накапливаются в буфере и переходят в читаемый снимок
только в `LateUpdate` translator'а.

```mermaid
sequenceDiagram
    participant S as Источник ввода
    participant St as PlayerInputState
    participant T as InputTranslator
    participant C as Потребитель

    Note over S,C: Кадр N, фаза Update
    S->>St: SetKey(Jump, true)
    St->>St: held = true, буфер нажатий = true
    C->>St: IsHeld(Jump)
    St-->>C: true
    C->>St: IsPressed(Jump)
    St-->>C: false

    Note over S,C: Кадр N, фаза LateUpdate
    T->>St: FrameSync()
    St->>St: снимок и буфер меняются местами
    St-->>C: OnPressedKey(Jump)

    Note over S,C: Кадр N+1, фаза Update
    C->>St: IsPressed(Jump)
    St-->>C: true
```

Отсюда два практических следствия:

- `IsHeld` виден в том же кадре, `IsPressed` и `IsReleased` — со следующего.
  Если реакция нужна кадр в кадр, подписывайтесь на `OnPressedKey`: событие приходит
  в `LateUpdate` того же кадра, в котором нажатие записано.
- Источники ввода должны писать до `LateUpdate` translator'а — то есть в `PRUpdate`,
  как это делают клавиатурный ввод, джойстик и бот. Источник, пишущий в `LateUpdate`,
  зависит от порядка выполнения скриптов и может опоздать на кадр.

## Реестр

`InputTranslator` — singleton-компонент, хранящий состояния по `InputGuid`.

```csharp
PlayerInputState input = InputTranslator.Instance.GetPlayer(player.InputGuid);

if (input.IsPressed(GameInputEnumerations.JumpAction))
    Jump();
```

| Метод | Что делает |
| --- | --- |
| `GetPlayer(guid)` | возвращает состояние, создавая его при первом обращении |
| `TryGetPlayer(guid, out state)` | то же, но без создания |
| `RemovePlayer(guid)` | убирает состояние и отписывает обработчики |
| `TryGetExisting(out translator)` | доступ к экземпляру без создания GameObject |

Событий четыре — `OnPressedKey`, `OnReleasedKey`, `OnChangeAxis`, `OnChangeVector`;
все передают `InputGuid`, поэтому подписчик может отфильтровать своего владельца.

Владельца можно завести или удалить прямо в обработчике события: текущий проход не ломается.

Снимая владельца со сцены, вызывайте `RemovePlayer`, иначе его состояние продолжит синхронизироваться каждый кадр. При уничтожении берите `TryGetExisting`: `Instance` создаст translator заново при выключении игры.

## Смотрите также

- [Entity](../@Entity/README.md) — `IPlayer.InputGuid`, по которому берётся состояние
- [Enumeration](../Models/Enumeration/README.md) — как объявить свой набор ключей ввода
