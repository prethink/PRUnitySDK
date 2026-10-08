# DOTweenEffects

Модуль связывает DOTween с логической паузой и системой `PRTimeScale`. Он содержит готовые компоненты эффектов и глобальный tracker для tween, создаваемых кодом.

## Зависимости

- PRUnitySDK Core;
- [DOTween](https://dotween.demigiant.com/).

DOTween должен быть установлен и настроен в проекте до использования модуля.

## Готовые компоненты

| Компонент | Назначение |
| --- | --- |
| `DoTweenMovementMonoBehaviour` | Перемещение смещением от себя, к точке родителя или к точке мира |
| `DoTweenRotateMonoBehaviour` | Поворот в заданные углы Эйлера |
| `DoTweenSpinMonoBehaviour` | Бесконечное вращение вокруг своей оси |
| `DoTweenScaleMonoBehaviour` | Изменение выбранных осей локального масштаба |
| `DoTweenFadeMonoBehaviour` | Прозрачность: затухание, проявление, мигание |

Общие Inspector-настройки:

- `Ease` — функция сглаживания;
- `Loop Type` — тип повторения;
- `Loop Count` — количество циклов, `-1` означает бесконечный цикл;
- `Duration` — длительность одного цикла в секундах;
- `Play Animation On Start` — создать эффект автоматически в `Start()`;
- `Ignore Pause Notify` — не приостанавливать эффект вместе с логической паузой.

Созданные компоненты используют глобальный слой `PRTimeScale`. Для другого слоя переопределите `GetTimeScaleLayer()`.

### Превью сглаживания

Под полем `Ease` рисуется график кривой: по названию вроде `OutSine` или `InOutBack`
не видно, будет там разгон, торможение или отскок за край. Делает это атрибут
`[EasePreview]` на поле и драйвер `Editor/EasePreviewDrawer`.

График считается через `DOVirtual.EasedValue`, как и сам DOTween. Тонкие линии — начало и конец движения; у `OutBack` и `OutElastic` кривая выходит за них, для этого в графике есть запас.

`Unset` в превью подменяется сглаживанием `DOTween.defaultEaseType`.

Атрибут годится любому полю типа `Ease`, не только в этом модуле.

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

## Movement

Значение `Movement` зависит от `Space`:

| `Space` | Вектор значит | Когда |
| --- | --- | --- |
| `Offset` (по умолчанию) | смещение от собственной позиции | покачивание, подскоки |
| `Local` | точка в координатах родителя | приехать в место внутри своей иерархии |
| `World` | точка в мировых координатах | цель задана в мире и от объекта не зависит |

Покачивание вверх-вниз — это `Offset`, `Movement = (0, 0.3, 0)`, `Loop Type = Yoyo`,
`Loop Count = -1`. С `World` тот же вектор утащил бы объект со своего места в точку
(0, 0.3, 0), то есть в центр карты.

`Offset` прибавляет смещение к текущей позиции и не записывает её. Поэтому объект качается вокруг места, где оказался, а два позиционных эффекта на одном объекте складываются. Подробнее в разделе «Смешивание повадок».

Перед `Kill()` компонент делает `Rewind()`, снимая свой вклад в объект.

## Spin

«Повернуться в угол» и «крутиться без конца» — разные задачи, и компонента два.

У `DoTweenSpinMonoBehaviour` нет целевого угла: задаются ось и градусы за цикл, поворот идёт прибавкой (`RotateMode.LocalAxisAdd`).

| Поле | Что задаёт |
| --- | --- |
| `Axis` | Ось вращения в координатах объекта |
| `Degrees Per Cycle` | Сколько градусов за цикл; длительность цикла — общий `Duration` |

При добавлении компонент сам проставляет `Ease = Linear`, `Loop Type = Restart`, `Loop Count = -1`.

## Fade

Прозрачность: затухание, проявление, мигание (`Loop Type = Yoyo`).

Цель ищется на самом объекте в таком порядке: `CanvasGroup` (гасит окно вместе с детьми), `Graphic` (картинка или подпись), `SpriteRenderer` (спрайт в мире). Ссылку можно задать вручную полем `Target`.

Без подходящей цели `CreateAnimation()` возвращает `null`, а `IsCreated` остаётся `false` —
так же ведёт себя масштаб, которому нечего менять.

Исходная прозрачность запоминается при первом создании и восстанавливается перед каждым следующим. `Yoyo` ходит между исходной прозрачностью и целью.

## Смешивание повадок

Компоненты можно вешать пачкой: парящий предмет, который крутится и пульсирует, — это три компонента.

Это работает, пока каждый компонент владеет своим свойством. Слоты:

| Слот | Чей |
| --- | --- |
| позиция | `Movement` |
| поворот | `Rotate`, `Spin` |
| масштаб | `Scale` |
| прозрачность | `Fade` |
| цвет | пока ничей |

Два эффекта на один слот дерутся: оба пишут значение каждый кадр, и виден тот, кто
обновился последним. Порядок при этом не задан.

Выход — blendable-твины DOTween: они не записывают значение, а прибавляют к нему,
поэтому складываются. На них уже сделаны `Movement` в режиме `Offset` и `Spin`
(`LocalAxisAdd` — тоже прибавка). Новый эффект, претендующий на занятый слот, обязан
быть таким же, иначе он тихо отменит соседа.

Абсолютные режимы (`Movement` с `Local` и `World`, `Rotate`) складывать нельзя: они задают точку, а не прибавку.

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
