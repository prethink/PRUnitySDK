# HookSystem

`HookSystem` — последовательная система перехвата действий в стиле AMX Mod X Ham Sandwich. В отличие от `EventBus`, listener не только получает уведомление, но и может изменить контекст, заменить результат или запретить оригинальное действие.

## Последовательность вызова

```text
Pre-hooks → Original action → Post-hooks
```

Если pre-hook устанавливает `Supercede`, оригинальное действие пропускается, но остальные pre- и post-hooks продолжают выполняться. Для явной остановки цепочки используется `StopPropagation()`.

## Результаты

Результаты расположены по возрастанию приоритета. Результат с меньшим приоритетом не может заменить уже установленный более сильный результат.

| Результат | Оригинальное действие | Результат контекста |
| --- | --- | --- |
| `Ignored` | Выполняется | Не заменяет оригинальный |
| `Handled` | Выполняется | Не заменяет оригинальный |
| `Override` | Выполняется | Заменяет оригинальный |
| `Supercede` | Не выполняется | Используется вместо оригинального |

`Modify()` отмечает контекст изменённым и повышает результат до `Handled`. Список изменивших контекст listeners доступен через `Modifiers`.

## Pre-hook

```csharp
public sealed class ArmorDamageHook : IHookListener<DamageHookEvent>
{
    public int Order => 100;

    public void HandleHook(DamageHookEvent context)
    {
        if (ShouldBlock(context))
            context.BlockDamage(this);
    }

    public void RegisterHook()
    {
        HookManager.Instance.Register(this);
    }

    public void UnRegisterHook()
    {
        HookManager.Instance.Unregister(this);
    }
}
```

Чем меньше `Order`, тем раньше вызывается listener. При одинаковом `Order` сохраняется порядок регистрации.

## Post-hook

Post-listener реализует `IHookPostListener<TArgs>`:

```csharp
public sealed class DamageLogHook : IHookPostListener<DamageHookEvent>
{
    public int Order => 1000;

    public void HandlePostHook(DamageHookEvent context)
    {
        Debug.Log($"Damage result: {context.DamageResult}; hook result: {context.Result}");
    }

    public void RegisterHook()
    {
        HookManager.Instance.Register(this);
    }

    public void UnRegisterHook()
    {
        HookManager.Instance.Unregister(this);
    }
}
```

Один класс может реализовать одновременно pre- и post-интерфейсы. Регистрировать его при этом нужно только один раз.

## Публикация

Когда оригинальное действие можно передать менеджеру, используйте перегрузку с callback:

```csharp
var context = new DamageHookEvent(attacker, weapon, victim, damage, DamageResult.NotHandled);

HookManager.Instance.Publish(
    context,
    args => ApplyDamage(args.DamageProvider));
```

Менеджер сам проверит `ShouldCallOriginal` между pre- и post-этапами.

Если основной код должен остаться у вызывающей стороны, можно использовать прежнюю перегрузку:

```csharp
var context = HookManager.Instance.Publish(new DamageHookEvent(...));

if (context.ShouldCallOriginal)
    ApplyDamage(context.DamageProvider);
```

В этой форме `Publish(context)` выполняет pre- и post-hooks подряд, поэтому post-listener не должен рассчитывать, что внешний основной код уже отработал. Для настоящих post-hooks предпочтительна перегрузка с `originalAction`.

## Остановка цепочки

`Supercede()` и `StopPropagation()` решают разные задачи:

```csharp
context.Supercede(this);   // запрещает оригинальное действие
context.StopPropagation(); // запрещает последующие listeners
```

`StopPropagation()` сам по себе не запрещает оригинальное действие. Чтобы сделать и то и другое, нужно вызвать оба метода.

## Регистрация и производительность

- Повторная регистрация одного экземпляра игнорируется.
- Listener можно удалить через `Unregister()`.
- Для каждого типа контекста строится отсортированный pipeline.
- Pipeline пересобирается только после изменения регистраций.
- Публикация выполняется по snapshot, поэтому регистрация или удаление listener внутри callback не повреждает текущий проход и вступает в силу со следующей публикации.

Не забывайте удалять listeners при завершении их жизненного цикла, например в `OnDisable()` или `Dispose()`.
