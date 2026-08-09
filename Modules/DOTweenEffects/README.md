# DOTweenEffects

Модуль связывает DOTween с логической паузой и системой `PRTimeScale`. Он содержит готовые компоненты эффектов и глобальный tracker для tween, создаваемых кодом.

## Зависимости

- PRUnitySDK Core;
- [DOTween](https://dotween.demigiant.com/).

DOTween должен быть установлен и настроен в проекте до использования модуля.

## Готовые компоненты

| Компонент | Назначение |
| --- | --- |
| `DoTweenMovementMonoBehaviour` | Перемещение к абсолютной мировой позиции |
| `DoTweenRotateMonoBehaviour` | Вращение к заданным углам Эйлера |
| `DoTweenScaleMonoBehaviour` | Изменение выбранных осей локального масштаба |

Общие Inspector-настройки:

- `Ease` — функция сглаживания;
- `Loop Type` — тип повторения;
- `Loop Count` — количество циклов, `-1` означает бесконечный цикл;
- `Duration` — длительность одного цикла в секундах;
- `Play Animation On Start` — создать эффект автоматически в `Start()`;
- `Ignore Pause Notify` — не приостанавливать эффект вместе с логической паузой.

Созданные компоненты используют глобальный слой `PRTimeScale`. Для другого слоя переопределите `GetTimeScaleLayer()`.

## Управление компонентом

```csharp
[SerializeField]
private DoTweenRotateMonoBehaviour rotationEffect;

private void PlayRotation()
{
    rotationEffect
        .SetDuration(0.4f)
        .SetEase(Ease.OutBack)
        .SetLoopCount(1);

    rotationEffect.SetRotateCoordinate(new Vector3(0f, 180f, 0f));
    rotationEffect.CreateAnimation();
}
```

`CreateAnimation()` убивает предыдущий tween компонента и создаёт новый. DOTween запускает созданный tween автоматически.

```csharp
rotationEffect.StopAnimation();    // Pause с сохранением прогресса
rotationEffect.StartAnimation();   // Play существующего tween
rotationEffect.DestroyAnimation(); // Kill и сброс IsCreated
```

`StartAnimation()` не создаёт отсутствующий tween. Сначала вызовите `CreateAnimation()` либо включите `Play Animation On Start`.

## Scale

В `DoTweenScaleMonoBehaviour` нулевая компонента целевого `Vector3` означает «не изменять эту ось»:

```csharp
scaleEffect.ChangeScale(new Vector3(2f, 0f, 2f));
scaleEffect.CreateAnimation();
```

Пример изменяет X и Z, сохраняя текущий Y. Из-за этой семантики готовый компонент не позволяет анимировать ось непосредственно к нулевому масштабу; для этого создайте специализированный эффект или обычный DOTween tween.

Если ни одна ось не требует изменения, `CreateAnimation()` возвращает `null`, а `IsCreated` остаётся `false`.

## Логическая пауза

Компонент автоматически подписывается на `EventBus` в `OnEnable()` и отписывается в `OnDisable()`.

При логической паузе:

- обычный эффект вызывает `Pause()`;
- после снятия паузы вызывает `Play()`;
- эффект с `Ignore Pause Notify` не изменяется.

При уничтожении компонента его tween убивается.

## PRTimeScale

Скорость эффекта определяется через:

```csharp
PRTimeScale.Instance.Resolve(GetTimeScaleLayer());
```

При изменении соответствующего слоя `tween.timeScale` обновляется автоматически. Глобальный слой учитывается в resolved-значении.

Пример собственного слоя:

```csharp
public sealed class UiScaleEffect : DoTweenScaleMonoBehaviour
{
    public override Enumeration GetTimeScaleLayer()
    {
        return PRTimeScaleEnumerationProvider.UI;
    }
}
```

## DoTweenTracker

Tracker применяется к tween, которые создаются напрямую кодом и не принадлежат готовому компоненту:

```csharp
Tween tween = transform
    .DOMove(targetPosition, 0.5f)
    .SetEase(Ease.OutQuad);

Guid tweenId = PRUnitySDK.Trackers.DoTween.Register(
    tween,
    layer: PRTimeScaleEnumerationProvider.Player,
    reactionOnPause: true);
```

`Register()`:

- создаёт `Guid`;
- назначает его как DOTween id;
- применяет resolved time scale;
- при необходимости включает реакцию на логическую паузу;
- автоматически удаляет запись после `Tween.Kill()`.

Принудительное завершение и удаление:

```csharp
PRUnitySDK.Trackers.DoTween.Kill(tweenId);
```

## Замена tween по известному id

`RegisterOrReplace()` полезен, когда одна игровая операция должна иметь не более одного активного tween:

```csharp
movementTweenId = PRUnitySDK.Trackers.DoTween.RegisterOrReplace(
    movementTweenId,
    transform.DOMove(targetPosition, duration),
    PRTimeScaleEnumerationProvider.Player);
```

Предыдущий tween с этим `Guid` будет убит и заменён новым.

## Поведение tracker при паузе

Для tween с `reactionOnPause: true` tracker запоминает, был ли tween запущен перед паузой. После снятия паузы возобновляются только ранее запущенные tween; вручную приостановленный tween не запускается автоматически.

Tween с `reactionOnPause: false` полностью игнорирует логическую паузу, но продолжает получать изменения своего слоя `PRTimeScale`.

## Рекомендации

- Не управляйте одним tween одновременно через компонент и `DoTweenTracker`.
- Для повторного создания component-эффекта используйте `CreateAnimation()` — предыдущий tween будет убит.
- Не меняйте DOTween id после регистрации в tracker.
- Для бесконечных циклов используйте `Loop Count = -1`.
- Учитывайте, что `Duration = 0` создаёт мгновенный tween.
- Если tween больше не нужен, вызывайте `Kill()` или уничтожайте владеющий им компонент.
