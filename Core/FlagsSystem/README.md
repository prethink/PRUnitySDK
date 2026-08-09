# Flags System

Система флагов объединяет независимые решения компонентов без прямых зависимостей между ними. Она состоит из трёх уровней:

- `FlagResolver` — обычный C#-контейнер влияний;
- `FlagResolverMono` — владелец локального resolver на `GameObject`;
- `FlagsManager` — глобальная агрегация project- и scene-resolvers.

## Модель разрешения

Каждый source может проголосовать за флаг:

```text
Deny > Allow > Unspecified
```

- хотя бы один живой `Deny` даёт итоговый `Deny`;
- если `Deny` нет, но есть `Allow`, итогом будет `Allow`;
- если влияний нет, resolver возвращает `Unspecified`.

Значение по умолчанию применяется только методом `Get`:

```csharp
bool canMove = resolver.Get(PlayerFlags.CanMove, defaultValue: true);
bool isRagdoll = resolver.Get(PlayerFlags.IsRagdoll, defaultValue: false);
```

Для агрегации нескольких resolver используйте `Resolve`, чтобы не спутать `Unspecified` с `Allow`:

```csharp
FlagDecision decision = resolver.Resolve(PlayerFlags.CanMove);
```

## Локальные флаги компонента

`FlagResolverMono` размещается на корневом объекте сущности. Компоненты могут использовать `Link` напрямую:

```csharp
FlagResolver flags = GetComponent<FlagResolverMono>().Link;

flags.Deny(PlayerFlags.CanMove, this);
flags.Deny(PlayerFlags.CanControl, this);

// Когда влияние больше не нужно:
flags.Remove(PlayerFlags.CanMove, this);
flags.Remove(PlayerFlags.CanControl, this);
```

Или convenience API самой MonoBehaviour-обёртки:

```csharp
FlagResolverMono flags = GetComponent<FlagResolverMono>();
flags.Deny(PlayerFlags.CanMove, this);
```

`FlagResolverMono` очищает resolver при уничтожении владельца.

## Persistent и frame influences

Обычные влияния сохраняются, пока их явно не удалить:

```csharp
resolver.Allow(key, source);
resolver.Deny(key, source);
resolver.Remove(key, source);
resolver.ClearSource(source);
```

Frame-влияния живут до `ClearFrameFlags()`:

```csharp
resolver.AllowFrame(PlayerFlags.CanJump, this);
resolver.DenyFrame(PlayerFlags.CanMove, iceEffect);

// В согласованной lifecycle-точке после потребителей:
resolver.ClearFrameFlags();
```

Persistent и frame-влияния хранятся раздельно. Один source может одновременно иметь оба влияния на один key; очистка frame-слоя не удаляет persistent-запись.

Совместимые методы сохранены:

```csharp
resolver.Add(key, source, true);       // Allow, persistent
resolver.Add(key, source, false);      // Deny, persistent
resolver.AddFrame(key, source, true);  // Allow, frame
```

Для нового кода предпочтительнее именованные `Allow`/`Deny`: они явно показывают намерение.

## Очистка source

Когда компонент управляет несколькими временными флагами, удобно удалить их вместе:

```csharp
protected override void OnDisable()
{
    resolver.ClearSource(this);
    base.OnDisable();
}
```

`ClearSource` удаляет persistent и frame-влияния source. Для точечного изменения используйте `Remove(key, source)`.

Уничтоженные Unity objects игнорируются при разрешении. `Cleanup()` физически удаляет такие записи. Обычный CLR-object необходимо удалять явно.

## События

Авторитетное событие сообщает трёхзначное решение:

```csharp
resolver.OnChangeFlagDecision += (key, decision) =>
{
    // decision: Unspecified, Allow или Deny
};
```

Оно вызывается только при реальном изменении итогового решения, а не при каждом обновлении отдельного source.

`OnChangeFlagState` оставлено для обратной совместимости:

```text
Allow                 → true
Deny или Unspecified  → false
```

Так как bool не может отличить `Deny` от `Unspecified`, новый код должен использовать `OnChangeFlagDecision`.

## Глобальные флаги

`FlagsManager` объединяет глобальный project resolver и зарегистрированные scene resolvers:

```text
ProjectFlags
SceneResolver A
SceneResolver B
       ↓
global Resolve/Get
```

`Deny` из любого слоя имеет абсолютный приоритет.

Глобальное влияние:

```csharp
PRUnitySDK.Managers.Flags.Deny(GameFlagsEnumerationProvider.UseGravity, source);

bool useGravity = PRUnitySDK.Managers.Flags.Get(
    GameFlagsEnumerationProvider.UseGravity,
    defaultValue: true);

PRUnitySDK.Managers.Flags.Remove(GameFlagsEnumerationProvider.UseGravity, source);
```

Полный project resolver доступен через `Global`:

```csharp
PRUnitySDK.Managers.Flags.Global.Allow(key, source);
```

Scene resolver регистрируется на время жизни сцены или режима:

```csharp
private readonly FlagResolver sceneFlags = new();

protected override void OnEnable()
{
    base.OnEnable();
    PRUnitySDK.Managers.Flags.AddSceneFlags(sceneFlags);
}

protected override void OnDisable()
{
    PRUnitySDK.Managers.Flags.RemoveSceneFlags(sceneFlags);
    sceneFlags.Clear();
    base.OnDisable();
}
```

## Рекомендации

- Используйте стабильный object как `source`, обычно `this` компонента.
- Не используйте строки и новые временные objects как source: потом их сложно удалить.
- Для возможностей с default `true` обычно нужен `Deny` (`CanMove`, `CanJump`).
- Для состояний с default `false` обычно нужен `Allow` (`IsRagdoll`, `IsSwimming`).
- Не используйте `HasAny() + Get()` для агрегации. Используйте `Resolve()`.
- Вызывайте `ClearFrameFlags()` после всех потребителей frame-флагов.
- В `OnDisable` очищайте persistent-влияния долгоживущего resolver.

## Пример PlayerController

Hook временно запрещает управление:

```csharp
public void OnHooked()
{
    flags.Deny(PlayerFlags.CanControl, this);
    flags.Deny(PlayerFlags.CanMove, this);
}

public void OnHookCompleted()
{
    flags.Remove(PlayerFlags.CanControl, this);
    flags.Remove(PlayerFlags.CanMove, this);
}
```

Swimming отключает gravity и объявляет состояние:

```csharp
public void OnEnter()
{
    flags.Deny(PlayerFlags.CanGravity, this);
    flags.Allow(PlayerFlags.IsSwimming, this);
}

public void OnExit()
{
    flags.ClearSource(this);
}
```
