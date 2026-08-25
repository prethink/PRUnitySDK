# PRTimeScale

`PRTimeScale` управляет независимыми слоями скорости времени. Глобальный слой влияет на
весь игровой мир, а дополнительные слои позволяют отдельно замедлять игрока, NPC или UI.
Система не изменяет `UnityEngine.Time.timeScale` автоматически.

## Стандартные слои

`PRTimeScaleEnumerationProvider` объявляет:

- `Global`;
- `Player`;
- `NPC`;
- `UI`.

Провайдер включает унаследованные значения, поэтому проект может расширить список своей
реализацией `EnumerationProviderBase`.

## Инициализация

Во время `PRUnitySDK.InitializeSDK()` вызывается `PRTimeScale.SingletonInitialize()`.
Все известные слои получают значение `1f`. До инициализации методы глобального разрешения
возвращают `DefaultTimeScale`.

## Установка значений

```csharp
PRTimeScale.Instance.SetGlobalTimeScale(0.5f);
PRTimeScale.Instance.SetTimeScale(PRTimeScaleEnumerationProvider.Player, 0.8f);
```

После изменения публикуется `IOnPRTimeScaleChange`:

```csharp
public class TimeScaleListener : MonoBehaviour, IOnPRTimeScaleChange
{
    private void OnEnable() => EventBus.Subscribe(this);
    private void OnDisable() => EventBus.Unsubscribe(this);

    public void OnPRTimeScaleChange(Enumeration layer, float value)
    {
        Debug.Log($"{layer}: {value}");
    }
}
```

## Получение итогового масштаба

```csharp
float playerScale = PRTimeScale.Instance.Resolve(
    PRTimeScaleEnumerationProvider.Player);
```

Результат зависит от `PRUnitySDK.Settings.Project.TimeScaleCombineMode`:

| Режим | Результат |
| --- | --- |
| `Multiply` | `global * layer` |
| `Max` | Максимальное из global и layer |
| `Min` | Минимальное из global и layer |
| `OverrideGlobal` | Значение layer без global |

`Resolve()` без аргумента и `Resolve(PRTimeScaleEnumerationProvider.Global)` возвращают
само значение глобального scale. Глобальный слой не комбинируется сам с собой: при значении
`0.5f` результат остаётся `0.5f`, а не `0.25f`. Режим комбинирования применяется только к
дополнительным слоям. Переданный дополнительный слой должен быть зарегистрирован при
инициализации, иначе прямое обращение к словарю завершится исключением.

## Временное изменение

```csharp
PRTimeScale.Instance.SetTimeScaleTemporarily(
    PRTimeScaleEnumerationProvider.Player,
    value: 0.25f,
    callBackTime: 2f);
```

Система запоминает предыдущее значение и восстанавливает его после задержки. Пока для
слоя действует временная задача, повторный запрос для этого слоя игнорируется. Длительность
считается по `PRTime.RealTime`, поэтому сама не замедляется вместе с игровым scale.

`IsTimeScaleTemporaryActive(layer)` позволяет проверить конкретный слой, а
`HasActiveTemporaryTimeScales` сообщает о наличии любой временной операции.

## Сброс

```csharp
PRTimeScale.Instance.Reset();
```

Все зарегистрированные слои возвращаются к `1f`, и для каждого публикуется событие.

В Play Mode теми же операциями можно управлять через секцию `PRTimeScale` окна
`PRUnitySDK/Tools/Debug Window`. Она отдельно показывает постоянные значения всех слоёв и
временный global override со scale и длительностью. Presets заполняют временное значение,
а `Apply` вызывает `SetGlobalTimeScaleTemporarily`. `Reset persistent values` недоступен,
пока действует временная операция. Отрицательный scale ограничивается нулём, а `NaN` и
бесконечность отклоняются. Счётчик `Event subscribers` помогает проверить, есть ли активные
получатели `IOnPRTimeScaleChange`.

## ITimeScaleLayer

Компонент может сообщить, какой слой влияет на него:

```csharp
public class NPCUnit : MonoBehaviour, ITimeScaleLayer
{
    public Enumeration GetTimeScaleLayer()
        => PRTimeScaleEnumerationProvider.NPC;
}
```

Затем потребитель получает итоговый scale через `Resolve(unit.GetTimeScaleLayer())`.

## Связь с PRTime

`PRTime.GameDeltaTime` и `GameFixedDeltaTime` автоматически применяют только глобальный
слой. Масштаб конкретного объекта нужно применять отдельно через `Resolve(layer)`.

`PRTimeScale` намеренно не изменяет `UnityEngine.Time.timeScale`. Обычный Unity `Animator`
реагирует на PR scale только через компонент-подписчик `IOnPRTimeScaleChange`, который
устанавливает `Animator.speed = PRTimeScale.Instance.Resolve(layer)`. Оба варианта установки —
постоянный и временный — публикуют одинаковое событие; временный дополнительно публикует его
при автоматическом восстановлении.

## Физика отдельного тела

Глобальный слой физика учитывает сама: хост тикает её через `Physics.Simulate(GameFixedDeltaTime)`,
где шаг уже умножен на глобальный масштаб. А вот слои PhysX не поддерживает — симуляция одна
на сцену, и гравитация для всех тел общая.

Драйвер добавляет телу разницу между его слоем и глобальным темпом. Вся логика лежит
в `RigidbodyTimeScaleDriverBase`, наследники отличаются только источником слоя:

| Компонент | Откуда берёт слой | Когда использовать |
| --- | --- | --- |
| `EntityTimeScaleDriver` | `EntityBase.GetTimeScaleLayer()` | объект — сущность: персонаж, NPC, враг |
| `RigidbodyTimeScaleDriver` | поле `timeScaleLayer` в инспекторе | снаряды, реквизит, платформы — всё без сущности |

```csharp
// сущность: слой берётся у неё, настраивать нечего
player.AddComponent<EntityTimeScaleDriver>();

// объект без сущности: слой задаётся явно
var driver = crate.AddComponent<RigidbodyTimeScaleDriver>();
driver.SetTimeScaleLayer(PRTimeScaleEnumerationProvider.Global);
```

Для сущностей предпочтителен `EntityTimeScaleDriver`: отдельное поле слоя пришлось бы
держать в согласии с сущностью вручную, а рассинхрон проявился бы как физика, идущая
не в том темпе, что анимация и скорость бега — они берут слой у сущности.

Сущность ищется в три шага, первый успешный побеждает:

1. поле `entity`, если задано в инспекторе;
2. `EntityLinkBase` на объекте или в родителях — штатный способ связать тело с сущностью,
   когда оно лежит не на её корне;
3. `EntityBase` в родителях.

Значение линка читается при каждом запросе слоя, а не запоминается: линк переназначается
в рантайме — объект берут из пула, меняется владелец. Явно заданная сущность имеет
приоритет над линком, поэтому `SetEntity()` перекрывает автоматический поиск,
а `SetEntityLink()` меняет источник.

Свой источник слоя добавляется наследованием от базы:

```csharp
public class WeaponTimeScaleDriver : RigidbodyTimeScaleDriverBase
{
    protected override Enumeration GetTimeScaleLayer() => weapon.Owner?.GetTimeScaleLayer();
}
```

`null` означает глобальный темп — драйвер в этом случае бездействует, потому что
глобальный масштаб уже заложен в шаг симуляции.

### Почему двух множителей мало одного

Замедление времени в `k` раз даёт разные коэффициенты для разных величин:

| Величина | Множитель | Почему |
| --- | --- | --- |
| скорость, импульс | `k` | входит в путь линейно |
| гравитация, сила, ускорение | `k²` | путь равен `v·(kt) + g·(kt)²/2` |

Если умножить только скорость, персонаж будет взлетать замедленно, а падать в обычном
темпе — прыжок сломается. Поэтому драйвер прикладывает поправку `g·(k²-1)`: гравитация
тела не отключается, при `k = 1` поправка равна нулю.

### Импульсы и скорости

Силы, которые прикладывает игровой код, драйвер сам масштабировать не может — используйте
расширения:

```csharp
rigidbody.AddScaledForce(Vector3.up * jumpForce, ForceMode.Impulse);
rigidbody.SetScaledVelocity(jumpVelocity);
float k = rigidbody.GetRelativeTimeScale();
```

Без драйвера на объекте они работают как обычные вызовы Rigidbody.

### Настройки драйвера

Общие для всех наследников:

| Поле | Назначение |
| --- | --- |
| `compensateGravity` | прикладывать поправку к гравитации |
| `scaleVelocityOnChange` | пересчитать скорость в момент смены масштаба — иначе уже летящее тело продолжит движение в прежнем темпе |

### Ограничения

- **`drag` остаётся в общем темпе.** Он затухает экспоненциально по шагам симуляции,
  и точная компенсация требует пересчёта коэффициента. Для персонажа расхождение
  незаметно, для брошенных предметов может быть видно.
- **Столкновения разрешаются за один общий шаг.** При `k` около 0.5 это выглядит нормально,
  при `k = 0.1` отскок замедленного тела будет резким.
- **Масштаб относительный.** Драйвер считает `Resolve(layer) / GetGlobalTimeScale()`:
  глобальное замедление уже в шаге симуляции, и учитывать его второй раз нельзя.
