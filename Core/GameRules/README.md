# GameRules

`GameRules` — глобальные ограничения характеристик. Это последний этап расчёта стата:
после того как применены базовые значения сущности и персональные модификаторы,
правила приводят результат в допустимые границы.

```text
EntityStatsBase → персональные модификаторы → GameRules → итоговое значение
```

Правила глобальны для всего проекта и применяются ко всем сущностям одинаково.

## Правило и модификатор — не одно и то же

Эту границу проще всего перепутать, поэтому она проговорена явно.

| | Модификатор | Правило |
| --- | --- | --- |
| Кому принадлежит | конкретной сущности | всему проекту |
| Откуда берётся | шляпа, питомец, бафф, экипировка | код `IStatRuleProvider` |
| Что выражает | «этот игрок быстрее на 20%» | «скорость не бывает меньше 0.2» |
| Когда меняется | в рантайме, постоянно | никогда после старта |
| Что происходит при конфликте | складываются или перемножаются | значение обрезается |
| Где живёт | `PropertyContainer` и сборщик модификаторов проекта | `GameRules` |

Практический критерий: если значение сломает игру или систему — это правило.
Если оно просто делает персонажа сильнее или слабее — это модификатор.

Правила задаются в коде, а не в ScriptableObject. Например, `MinValueRule(WalkSpeed, 0.2f)` нужен, потому что при нулевой скорости ломается контроллер.

## Состав

| Тип | Назначение |
| --- | --- |
| `GameRules` | Статический реестр: собирает правила при старте и применяет их |
| `IStatRuleProvider` | Поставщик набора правил; реализации находятся автоматически |
| `StatRuleBase` | Базовое правило: стат, приоритет, метод `Apply` |
| `MinValueRule` | Нижняя граница значения |
| `MaxValueRule` | Верхняя граница значения |

## Как добавить свои правила

Достаточно объявить класс — регистрировать его нигде не нужно:

```csharp
public class PlayerRules : IStatRuleProvider
{
    public string RuleName => "Player rules";

    public IEnumerable<StatRuleBase> GetRules()
    {
        return new List<StatRuleBase>
        {
            new MinValueRule(PlayerStatsEnumeration.PlayerHeight, 0.1f),
            new MaxValueRule(PlayerStatsEnumeration.PlayerHeight, 5f),

            new MinValueRule(PlayerStatsEnumeration.WalkSpeed, 0.2f),
            new MinValueRule(PlayerStatsEnumeration.JumpCount, 1f),
        };
    }
}
```

`GameRules.Initialize()` (вызывается из `PRUnitySDK.InitializeSDK`) находит все реализации `IStatRuleProvider` и раскладывает их правила по характеристикам. У провайдера должен быть **публичный конструктор без параметров**: типы без него, абстрактные и generic пропускаются с предупреждением в лог.

Ошибка одного провайдера (исключение в конструкторе или в `GetRules()`, `null` или правило без характеристики) пишется в лог и пропускается. Остальные наборы применяются, SDK не падает.

Сканируется только сборка, где объявлен `IStatRuleProvider` (то есть `Assembly-CSharp`), а не все сборки домена.

## Применение

```csharp
float speed = GameRules.ApplyStatRules(PlayerStatsEnumeration.WalkSpeed, rawSpeed);
int    count = GameRules.ApplyIntStatRule(PlayerStatsEnumeration.JumpCount, rawCount);
long   value = GameRules.ApplyLongStatRule(SomeStat, rawValue);
```

В обычном коде вызывать это напрямую не нужно — правила уже применяются внутри:

- `EntityStatsUtils.GetStat()` / `GetStatInt()` / `GetStatLong()`;
- `PropertyContainerBase.GetWithRules()`.

Повторно применять правила поверх результата этих методов не нужно и вредно.

Если для стата не задано ни одного правила (или передан `null`), значение возвращается
без изменений.

## Порядок применения

Правила одной характеристики применяются по возрастанию `Priority` — меньшее значение
раньше, как у `MethodHookAttribute.Order`. По умолчанию `100`. Правила с равным
приоритетом сохраняют порядок объявления в провайдере.

```csharp
new MaxValueRule(Stat, 100f, priority: 10),   // сначала потолок
new StepRule(Stat, 5f, priority: 20),         // затем округление до шага
```

Для пары Min и Max порядок не важен: при `min <= max` результат одинаков. Он важен для некоммутативных правил: умножения, округления, кривой.

## Диагностика

```csharp
bool ready = GameRules.IsInitialized;      // правила загружены
int total  = GameRules.RuleCount;          // сколько всего загружено
var stats  = GameRules.Stats;              // какие характеристики затронуты

foreach (StatRuleBase rule in GameRules.GetRules(PlayerStatsEnumeration.WalkSpeed))
    Debug.Log($"{rule.GetType().Name} priority={rule.Priority}");
```

`GetRules()` возвращает правила в порядке применения. По нему видно, почему значение обрезано, без чтения кода провайдеров.

### Окно PRUnitySDKDebug

Вкладка `Rules` показывает загруженные правила по характеристикам, в порядке применения, с приоритетом и параметрами. Блок `Apply rules to value` показывает, что вернёт `ApplyStatRules` для введённого числа и было ли оно обрезано. См. [Editor](../Editor/README.md).

## Новый тип правила

```csharp
public class StepRule : StatRuleBase
{
    private readonly float step;

    public StepRule(Enumeration stat, float step, int priority = 100)
        : base(stat, priority)
    {
        this.step = step;
    }

    public override float Apply(float value)
    {
        return Mathf.Round(value / step) * step;
    }
}
```

Если операция правила некоммутативна, задайте `priority` явно — см. раздел
[Порядок применения](#порядок-применения).

## Ограничения

- **Противоречивые правила не проверяются.** Два провайдера могут задать на один стат
  `Min(2)` и `Max(1)`. SDK не замечает конфликт, результат просто окажется бессмысленным.
- **Отсечение ничем не отмечается.** Бафф может не дать эффекта, потому что значение уже упёрлось в потолок, и никто этого не узнает. Правила характеристики видны через `GetRules()` или вкладку `Rules`, но срабатывание клампа в рантайме не логируется и в телеметрию не попадает.
- **Провайдер создаётся один раз при старте.** Правила, зависящие от состояния игры или
  настроек игрока, так задать нельзя — набор фиксируется на весь сеанс. Для повторной
  загрузки есть `Initialize()`, но список типов кэшируется до перезагрузки домена.
- **Правила только глобальные.** Границ уровня «для этого режима» или «для этой сущности»
  нет: реестр статический и один на проект.
- **Только `float`.** `ApplyIntStatRule` и `ApplyLongStatRule` — это округление результата,
  а не отдельная точность.
- **Расположение разнесено.** Сам `GameRules` лежит в `Core.Data/`, а интерфейс и правила — в `Core/GameRules/`.

## Смотрите также

- [PropertyContainer](../PropertyContainer/README.md) — персональные модификаторы, этап перед правилами
- [@Entity](../@Entity/README.md) — `EntityStatsBase` и `EntityStatsUtils`
