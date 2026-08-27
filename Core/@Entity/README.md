# Entity

Сущность — это любой значимый объект игрового мира: игрок, питомец, шляпа, портал,
подбираемый контейнер. `EntityBase` даёт им общий идентификатор, единое описание
(имя, иконка, локализация, качество), регистрацию в глобальном реестре, правила
уничтожения и работу с пулом.

Всё, что должно быть найдено по типу, посчитано, сохранено, уничтожено по окончании
раунда или показано в UI, — сущность. Декорации и служебные компоненты — нет.

## Карта модуля

| Папка | Что внутри |
| --- | --- |
| `EntityBase/` | `EntityBase` и четыре готовых варианта под разные способы задать описание |
| `EntityInfo/` | Имя, иконка, локализация, качество и механизм переопределения |
| `EntityManager/` | `EntityTracker` — глобальный реестр сущностей, выдача Id, статистика |
| `EntityStats/` | Базовые характеристики (`EntityStatsBase`) и расчёт итоговых (`EntityStatsUtils`) |
| `EntityContainer/` | Подбираемые контейнеры: ресурс, действие |
| `Player/` | `IPlayer`, `PlayerBase`, `PlayerTracker`, команды |
| `Initialize/` | `ReadySignal` — сигнал готовности с подпиской «поздних» слушателей |
| `Pickup/` | Интерфейсы подбора |
| корень | `IEntity`, `EntityLink`, время жизни, параметры уничтожения, слой времени |

## Базовая сущность

```csharp
public class Portal : EntityBase
{
    public override Enumeration EntityType => EntityTypeEnumerationProvider.Portal;

    public override string Name => "Portal";

    protected override void InitializeEntityInfo()
    {
        Info = new EntityInfoContainer(definition);
    }
}
```

Обязательны три вещи: тип (`EntityType`), имя (`Name`) и заполнение `Info`.
Всё остальное базовый класс берёт на себя.

### Что даёт базовый класс

| Возможность | Как работает |
| --- | --- |
| Идентификатор | `Id` выдаётся при регистрации в трекере, из `Start()` |
| Регистрация | Автоматически в `Start()`, снятие — при уничтожении |
| Описание | `Info` — контейнер с именем, иконкой, локализацией и качеством |
| Уничтожение | `DestroyEntity()` с учётом пула и настройки `EntityDisposeAction` |
| Пул | Реализация `IPoolable` через `PoolBehaviour` |
| Пауза и время | Наследуется от `PRMonoBehaviour`; слой времени — `GetTimeScaleLayer()` |
| Мониторы | Автоматически подхватывает `RigidBodyPauseMonitor` и `AnimatorPauseMonitor` |

### Какой базовый класс выбрать

| Класс | Откуда берётся описание | Когда использовать |
| --- | --- | --- |
| `CommonEntity` | Поля прямо на компоненте | Разовые объекты сцены, быстрый прототип |
| `ScriptableEntity` | Ссылка на ассет `EntityInfoBase` | Объект, описание которого переиспользуется |
| `EntityDefinition<T>` | Типизированный ассет-определение | Питомцы, шляпы — всё, у чего есть каталог |
| `RuntimeEntityBase` | Поля компонента, объект создаётся в рантайме | Контейнеры, дропы, временные объекты |

`EntityDefinition<T>` — основной путь для контентных сущностей: `PetEntity`, `HatEntity`
и `BrainrotEntity` построены именно так, а их определения дополнительно поставляют
модификаторы характеристик.

## Идентификатор

`Id` выдаётся при регистрации в реестре, а регистрация происходит в **`Start()`**.
Это важно: в `Awake()` и `InitializationComponents()` идентификатора ещё нет.

```csharp
protected override void InitializationComponents()
{
    base.InitializationComponents();
    // Здесь Id == 0 — использовать его нельзя.
}

public override void OnReadyScene()
{
    // Здесь Id уже назначен.
}
```

Подробности о том, в какой именно реестр попадает сущность и как ведут себя
идентификаторы, — в разделе [Реестры](#реестры).

## EntityInfo

`Info` — контейнер, отвечающий на четыре вопроса: как называется, как выглядит, как
переводится, какого качества. Он же поддерживает переопределение: базовое описание
берётся из определения сущности, а поверх может лечь второе.

```csharp
string title = entity.Info.GetName();
Sprite icon  = entity.Info.GetIcon();
string text  = entity.Info.GetLocalization();
QualityType q = entity.Info.GetQuality();
```

Порядок разрешения для каждого поля: функция-override → `Override`-описание → `Base`-описание.
Функции-override (`NameOverride`, `SpriteOverride`, …) позволяют подменить одно поле,
не трогая остальные, — например, показать в UI кастомное имя питомца.

`EntityUtils.GetEntityInfo()` собирает контейнер автоматически: базовым берёт саму сущность,
а переопределением — `IEntityInfoProvider`, найденный на объекте. Если ни одного описания
нет, метод бросает `InvalidOperationException` — сущность без описания считается ошибкой.

Готовая реализация переопределения — компонент `EntityInfoProvider`: повесьте его на объект
сущности и укажите ассет `EntityInfoBase`. Так одному экземпляру можно дать собственное имя
или иконку, не заводя отдельный тип сущности.

## Поиск сущности из компонента

Компонент, лежащий на дочернем объекте, находит свою сущность через `EntityLink`:

```csharp
[RequireComponent(typeof(EntityLinkBase))]
public class HealthBar : PRMonoBehaviour
{
    private EntityLinkBase link;

    protected override void InitializationComponents()
    {
        link = GetComponent<EntityLinkBase>();
        base.InitializationComponents();
    }

    private void Refresh()
    {
        if (link.Entity == null)
            return;

        // link.Entity — сущность этого объекта
    }
}
```

`EntityLink` ищет сущность сам: сначала в родителях, затем на своём объекте. Ссылку можно
задать и вручную в инспекторе — тогда автопоиск её не перезапишет. Типизированный вариант
`EntityLinkBase<T>` даёт `LinkedEntity` нужного типа и `TryGetEntity(out T)`.

Это же основной способ для систем SDK связать компонент с сущностью — так работает,
например, `EntityTimeScaleDriver`, который берёт слой времени из связанной сущности.

## Реестры

Сущности попадают в один из **двух независимых** реестров. Это ключевой факт про модуль,
и он не следует из названий:

| Реестр | Кто туда попадает | Доступ |
| --- | --- | --- |
| `EntityTracker` | Все сущности, **кроме игроков** | `PRUnitySDK.Trackers.Entities` |
| `PlayerTracker` | Только игроки (`PlayerBase` и наследники) | `PRUnitySDK.Trackers.Players` |

`PlayerBase` переопределяет `RegisterEntity()` и регистрирует себя **только** в
`PlayerTracker`, не вызывая базовую реализацию:

```csharp
protected override void RegisterEntity()
{
    PRUnitySDK.Trackers.Players.Register(this);
    OnPlayerInit?.Invoke(this);
}
```

Практические следствия, о которых легко забыть:

- `Trackers.Entities.Entities` **не содержит игроков**;
- `GetExactExistsEntityCount(EntityTypeEnumerationProvider.Player)` вернёт `0`;
- `EntityTracker.Clear()`, `ClearRound()` и `ClearSession()` игроков не трогают — для них
  те же операции нужно вызывать у `PlayerTracker`;
- статистика `RegisteredEntity` игроков не учитывает.

Если нужны «все объекты мира вместе с игроками» — берите оба списка.

### Когда происходит регистрация

| Момент | Что происходит |
| --- | --- |
| `Awake` / `InitializationComponents` | Регистрации ещё нет, `Id == 0` |
| `Start` | `RegisterEntity()` → выдача `Id` → сущность в реестре |
| Уход в пул | Регистрация **сохраняется**, меняется только `InPool` |
| Возврат из пула | Повторной регистрации нет, `Id` остаётся прежним |
| `OnDestroy` | `UnregisterEntity()` через `UnRegisterEventsOnDestroy()` |

Из-за этого объект в пуле продолжает считаться существующей сущностью — именно для этого
у трекера есть отдельные счётчики `GetEntityOnSceneCount()` и `GetEntityInPoolCount()`.

Если наследник переопределяет `RegisterEntity()` или `UnregisterEntity()`, он берёт на себя
и выбор реестра — как это делает `PlayerBase`. Забыть про `base` здесь не ошибка,
а способ сменить реестр; но и потерять регистрацию совсем так же легко.

### Идентификаторы

`Id` выдаёт общий `EntityIdGenerator` — сквозной счётчик, растущий от нуля. Оба трекера
берут Id из него, поэтому идентификаторы игроков и остальных сущностей не пересекаются.
Освобождённые Id **не переиспользуются**: удалённая сущность свой номер уносит с собой.

У игрока есть второй идентификатор — `PlayerId`, и он ведёт себя иначе: номера
переиспользуются, а локальным игрокам зарезервированы фиксированные значения
(`LocalPlayerOneId`, `LocalPlayerTwoId`).

### Подсчёт

```csharp
var tracker = PRUnitySDK.Trackers.Entities;

long pets       = tracker.GetExactExistsEntityCount(EntityTypeEnumerationProvider.Pet);
long petsOnScene = tracker.GetExactEntityOnSceneCount(EntityTypeEnumerationProvider.Pet);
long petsInPool  = tracker.GetExactEntityInPoolCount(EntityTypeEnumerationProvider.Pet);

long allContainers = tracker.GetInheritedExistsEntityCount(typeof(ContainerEntityBase));
```

`GetExact*` сравнивает `EntityType`, `GetInherited*` — CLR-тип с учётом наследования.
Считайте именно этими методами: они не создают промежуточных коллекций, в отличие от
свойства `Entities`, которое каждый раз возвращает новый список.

## Время жизни

Задаётся полем `LifeTime` в инспекторе:

| Значение | Когда уничтожается |
| --- | --- |
| `Infinity` | Никогда автоматически |
| `Scene` | Со сменой сцены |
| `Session` | По `ClearSession()` |
| `Round` | По `ClearRound()` |

`ClearSession()` чистит сущности сессии, а затем вызывает `ClearRound()`. Обе операции
уничтожают объект с `FullDestroy = true`, то есть игнорируют настройку пула. Вызывать их
нужно у обоих трекеров, если игроки тоже должны быть очищены.

## Уничтожение и пул

```csharp
entity.DestroyEntity();                                            // по настройке компонента
entity.DestroyEntity(new EntityDestroyOptions { FullDestroy = true }); // безусловно
```

Поведение задаёт поле `EntityDisposeAction` в инспекторе:

- `Destroy` — объект уничтожается;
- `HideInPool` — объект возвращается в пул.

`FullDestroy = true` игнорирует эту настройку и уничтожает объект даже из пула.
Если выбран `HideInPool`, но объект создан не через пул, SDK напишет предупреждение
и уничтожит его — молчаливой утечки не будет.

Объект, поднятый из пула, повторно проходит `InitializeEntity()`: при первом создании
инициализация идёт обычным путём, при переиспользовании — через `PoolBehaviour`.
Всё, что должно сбрасываться между использованиями, кладите именно туда, а не в `Awake`.

## Характеристики

Базовые значения хранит `EntityStatsBase` — ScriptableObject со словарём «стат → число»:

```csharp
public class PlayerStats : EntityStatsBase<PlayerStatsEnumeration> { }
```

Итоговое значение считает `EntityStatsUtils`, применяя три слоя по порядку:

```text
EntityStatsBase → персональные модификаторы → GameRules → результат
```

```csharp
float speed = EntityStatsUtils.GetStat(
    PlayerStatsEnumeration.WalkSpeed, Core.Stats, statCollector);

int jumps = EntityStatsUtils.GetStatInt(
    PlayerStatsEnumeration.JumpCount, Core.Stats, statCollector, 1);
```

`GetStatLong()` дополнительно защищает от выхода за границы типа и от `NaN`.
Округление всегда происходит **после** всех модификаторов и правил, а не в промежутке.

Важно: `GetStatInt` округляет к ближайшему целому. Для дробных величин (время, доли
секунды, множители) используйте `GetStat` — иначе значение вроде `0.15` превратится в `0`.

## Игроки

`IPlayer` расширяет `IEntity` очками, убийствами, смертями, командой, типом управления
(`Human` / `AI` / `NPC`) и характеристиками. `PlayerBase` добавляет события смены ника,
инициализации, изменения очков и учёт атакующего.

`PlayerTracker` ведёт отдельный учёт: сколько всего игроков, сколько людей, сколько
локальных. Локальным игрокам выделены фиксированные идентификаторы
(`LocalPlayerOneId`, `LocalPlayerTwoId`), а `MaxLocalPlayer` зависит от устройства —
на десктопе допускается двое, на мобильном один.

Команды — `DefaultTeam`, `RedTeam`, `BlueTeam` с фиксированными GUID из `TeamGuids`.

## Контейнеры

`ContainerEntityBase` — сущность, которую игрок подбирает касанием:

| Тип | Что выдаёт |
| --- | --- |
| `ResourceContainer` | Ресурс в заданном количестве — через `IPickupResource` |
| `ActionContainer` | Выполняет действие `IconActionBase` |

Кто может подобрать, задаётся флагами `PlayerTypeFlags` — например, чтобы AI не собирал
монеты. После успешного подбора контейнер уничтожает себя по своим правилам пула.

Подбор идёт через PR-хуки (`PROnTriggerEnter`, `PROnCollisionEnter`), поэтому во время
логической паузы не срабатывает. На паузе физика не симулируется вовсе — `Physics.Simulate`
вызывается из `PRFixedUpdate` хоста, — так что касание, случившееся при открытом окне,
не теряется: триггер сработает сразу после снятия паузы.

## Сигнал готовности

`ReadySignal` решает задачу «подписаться на событие, которое могло уже произойти»:

```csharp
PRUnitySDK.ReadySignal.SubscribeOnReady(() =>
{
    // Выполнится сразу, если SDK уже готов, иначе — в момент готовности.
});
```

Подписчик, пришедший после `SetReady()`, вызывается немедленно и синхронно. Дубликаты
подписок отсекаются, после срабатывания список очищается, а исключение в одном callback'е
не мешает остальным. Это правильный способ дождаться готовности вместо проверок в `Update`.

## Сущность без объекта

`GameEventEntity` — псевдосущность для урона и событий, у которых нет источника на сцене
(падение с высоты, утопление, зона). У неё `Id == -1`, она всегда «на сцене» и не попадает
в пул. Нужна там, где API требует `IEntity`, а виновника не существует.

## Ограничения

- **Переопределение описания задаётся только в инспекторе.** `EntityInfoProvider` хранит
  ссылку на ассет, а `EntityInfoContainer` собирается один раз при инициализации сущности:
  подменить описание в рантайме через `SetEntityInfo()` можно лишь до её регистрации.
- **`EntityManager` — пустой класс.** Наследник `SingletonProviderBase`, не содержит ничего
  и никем не используется; фактический реестр — `EntityTracker`, доступный через
  `EntityService.Instance` и `PRUnitySDK.Trackers.Entities`.
- **`Id` недоступен до `Start()`.** Регистрация идёт из `Start()`, поэтому в `Awake` и
  `InitializationComponents` идентификатор равен нулю.
- **Игроков нет в `EntityTracker`.** `PlayerBase` регистрирует себя только в `PlayerTracker`,
  поэтому обход всех сущностей игроков не увидит, а `Clear*` у `EntityTracker` их не затронет.
  Разделение намеренное, но легко приводит к «пропавшим» объектам при подсчётах.
- **Регистрация не снимается при уходе в пул.** Скрытая в пуле сущность остаётся в реестре;
  различать её нужно по `InPool`, а не по факту присутствия в списке.
- **Снимки коллекций аллоцируют.** `Entities`, `Players` и `TrackerBase.Elements` каждый раз
  возвращают новый `List` через `ToList()`. В `Update` такие обращения дают постоянный мусор —
  кешируйте результат или считайте через `GetExact*Count`.
- **Поиск стата линейный.** `EntityStatsBase<TEnum>.TryGet` перебирает весь словарь и на
  каждой итерации вызывает `ToEnumeration()`. Для статов, читаемых каждый кадр, это заметно.
- **Логика `DestroyEntity` непрозрачна.** Три ветки со сложными условиями и `throw new
  NotImplementedException()` в конце; последняя ветка при текущем `EntityDisposeAction`
  недостижима, но добавление нового значения enum приведёт к исключению в рантайме.
- **`IEntity.gameObject` объявлен со строчной буквы** — намеренно, чтобы совпадать с
  Unity-свойством и не конфликтовать при реализации в `MonoBehaviour`. Для не-Unity
  реализаций (`GameEventEntity`) объект создаётся фабрикой по требованию.
- **`EntityStatsUtils` зависит от `StatModifierCollector`** — типа из `PRUnitySDKPrivate`,
  то есть ядро ссылается на приватный модуль. Развязывается интерфейсом вроде
  `IStatModifierSource`.

## Смотрите также

- [Trackers](../Trackers/README.md) — общий контракт реестров, камеры, окна и уведомители
- [PRMonoBehaviour](../PRMonoBehaviour/README.md) — базовый lifecycle, пауза, физические хуки
- [PropertyContainer](../PropertyContainer/README.md) — персональные модификаторы характеристик
- [GameRules](../GameRules/README.md) — глобальные ограничения статов
- [PRTimeScale](../PRTimeScale/README.md) — слои времени и `EntityTimeScaleDriver`
- [Enumeration](../Models/Enumeration/README.md) — ключи типов, статов и слоёв
